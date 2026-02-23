public class Edge
{
    public int To;
    public int Capacity;
    public int Cost;
    public int Rev;

    public Edge(int to, int capacity, int cost, int rev)
    {
        To = to;
        Capacity = capacity;
        Cost = cost;
        Rev = rev;
    }
}