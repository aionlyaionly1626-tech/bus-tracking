using MedicalAI.Core.Models;

namespace MedicalAI.Core.Contracts;

public interface IMedicalInferenceEngine
{
	/// <summary>
	/// Runs inference on image data read from the stream's current position.
	/// The caller retains ownership of the stream.
	/// </summary>
	Task<DiagnosticResult> PredictAsync(Stream imageStream, CancellationToken cancellationToken = default);

	/// <summary>
	/// Runs inference on encoded image bytes.
	/// </summary>
	Task<DiagnosticResult> PredictAsync(
		byte[] imageBytes,
		CancellationToken cancellationToken = default);
}
