using MedicalAI.Core.Models;
using SkiaSharp;

namespace MedicalAI.Inference.Imaging;

public sealed class SkiaImageProcessor
{
	public float[] PreprocessToNchw(
		byte[] imageBytes,
		int inputWidth,
		int inputHeight,
		ReadOnlySpan<float> channelMeans,
		ReadOnlySpan<float> channelStandardDeviations)
	{
		ArgumentNullException.ThrowIfNull(imageBytes);

		if (imageBytes.Length == 0)
		{
			throw new ArgumentException("Image data cannot be empty.", nameof(imageBytes));
		}

		ValidateDimensions(inputWidth, inputHeight);
		ValidateNormalization(channelMeans, channelStandardDeviations);

		using var source = Decode(imageBytes);
		var imageInfo = new SKImageInfo(inputWidth, inputHeight, SKColorType.Rgba8888, SKAlphaType.Opaque);
		using var resized = source.Resize(
			imageInfo,
			new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));

		if (resized is null)
		{
			throw new InvalidOperationException("SkiaSharp could not resize the decoded image.");
		}

		var planeSize = checked(inputWidth * inputHeight);
		var tensor = new float[checked(3 * planeSize)];

		for (var y = 0; y < inputHeight; y++)
		{
			for (var x = 0; x < inputWidth; x++)
			{
				var pixel = resized.GetPixel(x, y);
				var pixelIndex = y * inputWidth + x;

				tensor[pixelIndex] = Normalize(pixel.Red, channelMeans[0], channelStandardDeviations[0]);
				tensor[planeSize + pixelIndex] = Normalize(pixel.Green, channelMeans[1], channelStandardDeviations[1]);
				tensor[(2 * planeSize) + pixelIndex] =
					Normalize(pixel.Blue, channelMeans[2], channelStandardDeviations[2]);
			}
		}

		return tensor;
	}

	public HeatmapOverlay CreateHeatmapOverlay(
		byte[] imageBytes,
		float[,] activationMatrix,
		float opacity = 0.4f,
		HeatmapImageFormat imageFormat = HeatmapImageFormat.Png,
		string colormapId = "jet")
	{
		ArgumentNullException.ThrowIfNull(imageBytes);
		ArgumentNullException.ThrowIfNull(activationMatrix);
		ArgumentException.ThrowIfNullOrWhiteSpace(colormapId);

		if (imageBytes.Length == 0)
		{
			throw new ArgumentException("Image data cannot be empty.", nameof(imageBytes));
		}

		if (activationMatrix.GetLength(0) == 0 || activationMatrix.GetLength(1) == 0)
		{
			throw new ArgumentException("Activation matrix must have non-zero dimensions.", nameof(activationMatrix));
		}

		if (activationMatrix.Cast<float>().Any(value => !float.IsFinite(value)))
		{
			throw new ArgumentException("Activation matrix values must be finite.", nameof(activationMatrix));
		}

		if (!float.IsFinite(opacity) || opacity is < 0 or > 1)
		{
			throw new ArgumentOutOfRangeException(nameof(opacity), "Opacity must be between 0 and 1.");
		}

		if (!Enum.IsDefined(imageFormat))
		{
			throw new ArgumentOutOfRangeException(nameof(imageFormat), "Unsupported heatmap image format.");
		}

		if (!string.Equals(colormapId, "jet", StringComparison.OrdinalIgnoreCase))
		{
			throw new ArgumentException("Only the 'jet' colormap is supported.", nameof(colormapId));
		}

		using var source = Decode(imageBytes);
		var outputInfo = new SKImageInfo(source.Width, source.Height, SKColorType.Rgba8888, SKAlphaType.Opaque);
		using var output = new SKBitmap(outputInfo);
		var activationHeight = activationMatrix.GetLength(0);
		var activationWidth = activationMatrix.GetLength(1);
		var minimum = activationMatrix.Cast<float>().Min();
		var maximum = activationMatrix.Cast<float>().Max();

		for (var y = 0; y < source.Height; y++)
		{
			var activationY = (int)((long)y * activationHeight / source.Height);

			for (var x = 0; x < source.Width; x++)
			{
				var activationX = (int)((long)x * activationWidth / source.Width);
				var activation = activationMatrix[activationY, activationX];
				var intensity = maximum == minimum ? 0f : (activation - minimum) / (maximum - minimum);
				var color = GetJetColor(intensity);
				var blendOpacity = opacity * intensity;
				var original = source.GetPixel(x, y);

				output.SetPixel(
					x,
					y,
					new SKColor(
						BlendChannel(original.Red, color.Red, blendOpacity),
						BlendChannel(original.Green, color.Green, blendOpacity),
						BlendChannel(original.Blue, color.Blue, blendOpacity)));
			}
		}

		using var image = SKImage.FromBitmap(output);
		var encodedFormat = imageFormat == HeatmapImageFormat.Png
			? SKEncodedImageFormat.Png
			: SKEncodedImageFormat.Jpeg;
		using var encoded = image.Encode(encodedFormat, quality: 95);

		if (encoded is null)
		{
			throw new InvalidOperationException($"SkiaSharp could not encode the heatmap as {imageFormat}.");
		}

		return new HeatmapOverlay(activationMatrix, encoded.ToArray(), imageFormat, "jet");
	}

	private static SKBitmap Decode(byte[] imageBytes) =>
		SKBitmap.Decode(imageBytes) ?? throw new InvalidDataException("Image data is not a supported encoded image.");

	private static void ValidateDimensions(int width, int height)
	{
		if (width <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(width), "Input width must be positive.");
		}

		if (height <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(height), "Input height must be positive.");
		}
	}

	private static void ValidateNormalization(
		ReadOnlySpan<float> channelMeans,
		ReadOnlySpan<float> channelStandardDeviations)
	{
		if (channelMeans.Length != 3 || channelStandardDeviations.Length != 3)
		{
			throw new ArgumentException("Image normalization requires exactly three RGB means and standard deviations.");
		}

		for (var channel = 0; channel < 3; channel++)
		{
			if (!float.IsFinite(channelMeans[channel]) || !float.IsFinite(channelStandardDeviations[channel]))
			{
				throw new ArgumentException("Normalization values must be finite.");
			}

			if (channelMeans[channel] is < 0 or > 1)
			{
				throw new ArgumentOutOfRangeException(nameof(channelMeans), "Channel means must be between 0 and 1.");
			}

			if (channelStandardDeviations[channel] <= 0)
			{
				throw new ArgumentOutOfRangeException(
					nameof(channelStandardDeviations),
					"Channel standard deviations must be positive.");
			}
		}
	}

	private static float Normalize(byte value, float mean, float standardDeviation) =>
		(value / 255f - mean) / standardDeviation;

	private static SKColor GetJetColor(float intensity)
	{
		var red = Math.Clamp(1.5f - MathF.Abs((4 * intensity) - 3), 0, 1);
		var green = Math.Clamp(1.5f - MathF.Abs((4 * intensity) - 2), 0, 1);
		var blue = Math.Clamp(1.5f - MathF.Abs((4 * intensity) - 1), 0, 1);

		return new SKColor(ToByte(red), ToByte(green), ToByte(blue));
	}

	private static byte BlendChannel(byte original, byte overlay, float opacity) =>
		(byte)Math.Clamp((int)MathF.Round((original * (1 - opacity)) + (overlay * opacity)), 0, 255);

	private static byte ToByte(float value) =>
		(byte)Math.Clamp((int)MathF.Round(value * byte.MaxValue), 0, byte.MaxValue);
}
