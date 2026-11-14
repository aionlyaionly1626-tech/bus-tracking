namespace MedicalAI.Core.Models;

public sealed record HeatmapOverlay
{
	public HeatmapOverlay(
		float[,] activationMatrix,
		byte[] imageBytes,
		HeatmapImageFormat imageFormat,
		string colormapId)
	{
		ArgumentNullException.ThrowIfNull(activationMatrix);
		ArgumentNullException.ThrowIfNull(imageBytes);
		ArgumentException.ThrowIfNullOrWhiteSpace(colormapId);

		if (activationMatrix.GetLength(0) == 0 || activationMatrix.GetLength(1) == 0)
		{
			throw new ArgumentException("Activation matrix must have non-zero dimensions.", nameof(activationMatrix));
		}

		if (activationMatrix.Cast<float>().Any(value => !float.IsFinite(value)))
		{
			throw new ArgumentException("Activation matrix values must be finite.", nameof(activationMatrix));
		}

		if (imageBytes.Length == 0)
		{
			throw new ArgumentException("Encoded heatmap image cannot be empty.", nameof(imageBytes));
		}

		if (!Enum.IsDefined(imageFormat))
		{
			throw new ArgumentOutOfRangeException(nameof(imageFormat), "Unsupported heatmap image format.");
		}

		ActivationMatrix = (float[,])activationMatrix.Clone();
		ImageBytes = (byte[])imageBytes.Clone();
		ImageFormat = imageFormat;
		ColormapId = colormapId;
	}

	public float[,] ActivationMatrix { get; }

	public byte[] ImageBytes { get; }

	public HeatmapImageFormat ImageFormat { get; }

	public string ColormapId { get; }
}
