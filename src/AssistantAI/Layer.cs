namespace Andronix.AssistantAI;

public class Layer
{
    private readonly int _inputCount;
    private readonly int _count;
    private readonly List<float[]> _weights;
    private float[] _values;
    private float[] _deltas;
    private float _bias;

    public Layer(int inputCount, int count)
    {
        _inputCount = 1 < inputCount ? inputCount : throw new ArgumentException("Input count must be greater than 0");
        _count = 1 < count ? count : throw new ArgumentException("Count must be greater than 0");

        _weights = new List<float[]>();
        _values = new float[count];
        _deltas = new float[count];
    }

    public float[] Values
    { 
        get => _values;
        set => _values = value;
    }

    public int Count => _count;

    public float[] Deltas => _deltas;

    public IList<float[]> Weights => _weights;

    public float Bias
    {
        get => _bias;
        set => _bias = value;
    }
}
