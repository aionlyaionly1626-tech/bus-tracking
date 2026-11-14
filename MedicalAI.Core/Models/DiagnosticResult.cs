using System.Collections.ObjectModel;

namespace MedicalAI.Core.Models;

public sealed record DiagnosticResult
{
	public DiagnosticResult(
		string predictedClass,
		double confidenceScore,
		IReadOnlyDictionary<string, double> rawProbabilities,
		DateTimeOffset timestamp,
		double processingLatencyMs)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(predictedClass);
		ArgumentNullException.ThrowIfNull(rawProbabilities);

		if (!double.IsFinite(confidenceScore) || confidenceScore is < 0 or > 1)
		{
			throw new ArgumentOutOfRangeException(nameof(confidenceScore), "Confidence must be between 0 and 1.");
		}

		if (!rawProbabilities.TryGetValue(predictedClass, out var predictedProbability))
		{
			throw new ArgumentException("Raw probabilities must contain the predicted class.", nameof(rawProbabilities));
		}

		if (Math.Abs(predictedProbability - confidenceScore) > 1e-6)
		{
			throw new ArgumentException("Confidence must match the predicted class probability.", nameof(confidenceScore));
		}

		if (rawProbabilities.Any(probability =>
			string.IsNullOrWhiteSpace(probability.Key) ||
			!double.IsFinite(probability.Value) ||
			probability.Value is < 0 or > 1))
		{
			throw new ArgumentException("Raw probabilities must have non-empty labels and values between 0 and 1.", nameof(rawProbabilities));
		}

		if (!double.IsFinite(processingLatencyMs) || processingLatencyMs < 0)
		{
			throw new ArgumentOutOfRangeException(
				nameof(processingLatencyMs),
				"Processing latency must be a finite, non-negative number.");
		}

		PredictedClass = predictedClass;
		ConfidenceScore = confidenceScore;
		RawProbabilities = new ReadOnlyDictionary<string, double>(
			new Dictionary<string, double>(rawProbabilities, StringComparer.Ordinal));
		Timestamp = timestamp.ToUniversalTime();
		ProcessingLatencyMs = processingLatencyMs;
	}

	public string PredictedClass { get; }

	public double ConfidenceScore { get; }

	public IReadOnlyDictionary<string, double> RawProbabilities { get; }

	public DateTimeOffset Timestamp { get; }

	public double ProcessingLatencyMs { get; }
}
