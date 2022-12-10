using System.Diagnostics;
using LiteDB;

namespace Pedantic.Genetics
{
    public class ChessWeights
    {
        public const int MAX_WEIGHTS = 800;
        public const int MAX_PARENTS = 2;

        [BsonCtor]
        public ChessWeights(ObjectId _id, bool isActive, bool isImmortal, int age, ObjectId[] parents, int wins,
            int draws, int losses, short[] weights, DateTime updatedOn)
        {
            ChessWeightsId = _id;
            IsActive = isActive;
            IsImmortal = isImmortal;
            Age = age;
            Parents = new ObjectId[parents.Length];
            Array.Copy(parents, Parents, MAX_PARENTS);
            Wins = wins;
            Draws = draws;
            Losses = losses;
            Weights = new short[MAX_WEIGHTS];
            Array.Copy(weights, Weights, MAX_WEIGHTS);
            UpdatedOn = updatedOn;
        }

        public ChessWeights(short[] weights)
        {
            ChessWeightsId = ObjectId.NewObjectId();
            IsActive = true;
            IsImmortal = false;
            Age = 0;
            Wins = 0;
            Losses = 0;
            Draws = 0;
            Array.Copy(weights, Weights, MAX_WEIGHTS);
            UpdatedOn = CreatedOn;
        }

        public ObjectId ChessWeightsId { get; set; }
        public bool IsActive { get; set; }
        public bool IsImmortal { get; set; }
        public int Age { get; set; }
        public ObjectId[] Parents { get; set; } = new ObjectId[MAX_PARENTS];
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Draws { get; set; }
        public short[] Weights { get; } = new short[MAX_WEIGHTS];
        public DateTime UpdatedOn { get; set; }

        [BsonIgnore] 
        public DateTime CreatedOn => ChessWeightsId.CreationTime;

        [BsonIgnore]
        public double NormalizedScore => (double)(Wins * 2 + Draws) / GamesPlayed;

        [BsonIgnore]
        public int GamesPlayed => Wins + Draws + Losses;
    }
}