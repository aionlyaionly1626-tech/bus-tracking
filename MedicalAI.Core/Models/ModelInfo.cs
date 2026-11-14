namespace MedicalAI.Core.Models;

public sealed record ModelInfo
{
	public ModelInfo(string architectureName, int inputWidth, int inputHeight, ModelPrecision precision)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(architectureName);

		if (inputWidth <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(inputWidth), "Input width must be positive.");
		}

		if (inputHeight <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(inputHeight), "Input height must be positive.");
		}

		if (!Enum.IsDefined(precision))
		{
			throw new ArgumentOutOfRangeException(nameof(precision), "Unsupported model precision.");
		}

		ArchitectureName = architectureName;
		InputWidth = inputWidth;
		InputHeight = inputHeight;
		Precision = precision;
	}

	public string ArchitectureName { get; }

	public int InputWidth { get; }

	public int InputHeight { get; }

	public ModelPrecision Precision { get; }
}
