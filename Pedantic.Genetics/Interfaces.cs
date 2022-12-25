using LiteDB;

namespace Pedantic.Genetics
{
    public interface IChromosome<T>
    {
        public ObjectId Id { get; }
        public double Score { get; }
        public int Age { get; }

        public static abstract T CreateRandom();
        public static abstract (T child1, T child2) CrossOver(T parent1, T parent2, bool checkMutation);
        public static abstract short NextWeight(int n);
    }

    public interface IMessageHandler
    {
        public void MessageHandler(Message msg, GeneticsRepository? rep = null);
    }

}