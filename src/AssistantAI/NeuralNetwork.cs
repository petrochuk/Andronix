using System.Numerics.Tensors;

namespace Andronix.AssistantAI;

public class NeuralNetwork
{
    private readonly List<Layer> _layers;
    private readonly int _layerCount;
    private readonly int _inputCount;
    public float _learningRate = 0.6f;

    public NeuralNetwork(int inputCount, int layerCount)
    {
        _layerCount = 2 <= layerCount ? layerCount : throw new ArgumentException("Layer count must be greater than 1");
        _inputCount = 1 < inputCount ? inputCount : throw new ArgumentException("Input count must be greater than 0");

        _layers = new List<Layer>();
        for (int i = 0; i < _layerCount; i++)
            _layers.Add(new Layer(inputCount, inputCount));

        InitWeights();
    }

    public IReadOnlyList<Layer> Layers => _layers;

    public Layer Input => _layers[0];

    public Layer Output => _layers[^1];

    /// <summary>
    /// Initializes the weights of the network to the identity matrix.
    /// Output equals input.
    /// </summary>
    private void InitWeights()
    {
        for (int i = 0; i < _layerCount; i++)
        {
            _layers[i].Bias = 0;
            for (nint j = 0; j < _inputCount; j++)
            {
                var weights = new float[_inputCount];
                weights[j] = 1;
                _layers[i].Weights.Add(weights);
            }
        }
    }

    public void FeedForwardReLU(float[] inputs)
    {
        if (inputs.Length != _inputCount)
            throw new ArgumentException("Input length must match input layer length");

        var input = _layers[0];
        input.Values = inputs;

        for (int i = 0; i < _layerCount - 1; i++)
        {
            input = _layers[i];
            var output = _layers[i+1];
            for (int j = 0; j < output.Count; j++)
            {
                output.Values[j] = TensorPrimitives.Dot(input.Values, output.Weights[j]) + output.Bias;
                output.Values[j] = 0 < output.Values[j] ? output.Values[j] : 0.01f * output.Values[j];
            }
        }
    }
    
    public void FeedForwardSigmoid(float[] inputs)
    {
        if (inputs.Length != _inputCount)
            throw new ArgumentException("Input length must match input layer length");

        var input = _layers[0];
        input.Values = inputs;

        for (int i = 0; i < _layerCount - 1; i++)
        {
            input = _layers[i];
            var output = _layers[i+1];
            for (int j = 0; j < output.Count; j++)
            {
                output.Values[j] = TensorPrimitives.Dot(input.Values, output.Weights[j]) + output.Bias;
            }
            TensorPrimitives.Sigmoid(output.Values, output.Values);
        }
    }

    public float BackPropagateReLU(float[] inputs, float[] expected)
    {
        FeedForwardReLU(inputs);

        // First pass to calculate deltas on output
        var output = _layers[^1];
        TensorPrimitives.Subtract(output.Values, expected, output.Deltas);
        var error = TensorPrimitives.SumOfSquares(output.Deltas) / _inputCount;

        for (int i = 0; i < output.Count; i++)
        {
            output.Deltas[i] = 0 < output.Deltas[i] ? output.Deltas[i] : 0.01f;
        }

        // Calculate deltas for hidden layers
        for (int i = _layerCount - 2; i > 0; i--)
        {
            var layer = _layers[i];
            var nextLayer = _layers[i + 1];
            for (int j = 0; j < layer.Count; j++)
            {
                layer.Deltas[j] = TensorPrimitives.Dot(nextLayer.Deltas, nextLayer.Weights[j]);
                layer.Deltas[i] = 0 < layer.Deltas[i] ? layer.Deltas[i] : 0.01f;
            }
        }

        // Adjust weights and biases for output layer
        var input = _layers[^2];
        for (int i = 0; i < output.Count; i++)
        {
            output.Bias -= output.Deltas[i] * _learningRate;
            for (int j = 0; j < _inputCount; j++)
            {
                output.Weights[i][j] -= output.Deltas[i] * input.Values[j] * _learningRate;
            }
        }

        // Adjust weights and biases for hidden layers
        for (int i = _layerCount - 2; i > 0; i--)
        {
            input = _layers[i - 1];
            var layer = _layers[i];
            for (int j = 0; j < layer.Count; j++)
            {
                layer.Bias -= layer.Deltas[j] * _learningRate;
                for (int k = 0; k < _inputCount; k++)
                {
                    layer.Weights[j][k] -= layer.Deltas[j] * input.Values[k] * _learningRate;
                }
            }
        }

        return error;
    }

    public float BackPropagateSigmoid(float[] inputs, float[] expected)
    {
        FeedForwardSigmoid(inputs);

        // First pass to calculate deltas on output
        var output = _layers[^1];
        TensorPrimitives.Subtract(output.Values, expected, output.Deltas);
        var error = TensorPrimitives.SumOfSquares(output.Deltas) / _inputCount;

        TensorPrimitives.Multiply(output.Deltas, output.Values, output.Deltas);
        TensorPrimitives.AddMultiply(output.Values, -1, output.Deltas, output.Deltas);
        TensorPrimitives.Multiply(output.Deltas, -1, output.Deltas);

        // Calculate deltas for hidden layers
        for (int i = _layerCount - 2; i > 0; i--)
        {
            var layer = _layers[i];
            var nextLayer = _layers[i + 1];
            for (int j = 0; j < layer.Count; j++)
            {
                layer.Deltas[j] = TensorPrimitives.Dot(nextLayer.Deltas, nextLayer.Weights[j]) * layer.Values[j] * (1 - layer.Values[j]);
            }
        }

        // Adjust weights and biases for output layer
        var input = _layers[^2];
        for (int i = 0; i < output.Count; i++)
        {
            output.Bias -= output.Deltas[i] * _learningRate;
            for (int j = 0; j < _inputCount; j++)
            {
                output.Weights[i][j] -= output.Deltas[i] * input.Values[j] * _learningRate;
            }
        }

        // Adjust weights and biases for hidden layers
        for (int i = _layerCount - 2; i > 0; i--)
        {
            input = _layers[i - 1];
            var layer = _layers[i];
            for (int j = 0; j < layer.Count; j++)
            {
                layer.Bias -= layer.Deltas[j] * _learningRate;
                for (int k = 0; k < _inputCount; k++)
                {
                    layer.Weights[j][k] -= layer.Deltas[j] * input.Values[k] * _learningRate;
                }
            }
        }

        return error;
    }
}