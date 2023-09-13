using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pedantic.Tuning
{
    public class MiniBatch
    {
        internal const int SMALLEST_BATCH_COUNT = 2;
        internal const int STARTING_BATCH_COUNT = 5;
        internal const int SMALLEST_BATCH_SIZE = 2500;

        private int currentBatchCount;
        private int currentBatchSize;
        private int currentBatch;
        private readonly int totalData;

        public MiniBatch(int totalData)
        {
            currentBatchCount = STARTING_BATCH_COUNT;
            if (totalData / currentBatchCount > SMALLEST_BATCH_SIZE)
            {
                while (totalData / (currentBatchCount * 2) > SMALLEST_BATCH_SIZE)
                {
                    currentBatchCount *= 2;
                }
                currentBatchSize = totalData / currentBatchCount;
            }
            else
            {
                currentBatchCount = 1;
                currentBatchSize = totalData;
            }
            currentBatch = 0;
            this.totalData = totalData;
        }

        public int BatchCount => currentBatchCount;
        public int BatchSize => currentBatchSize;
        public (int Start, int End) Batch
        {
            get
            {
                int index = currentBatch % currentBatchCount;
                int start = index * currentBatchSize;
                int end = start + currentBatchSize;
                if (index == currentBatchCount - 1)
                {
                    end = totalData;
                }
                return (Start: start, End: end);
            }
        }

        public void NextBatch()
        {
            currentBatch++;
        }

        public bool Increment()
        {
            if (currentBatchCount / 2 >= SMALLEST_BATCH_COUNT)
            {
                currentBatchCount /= 2;
                currentBatchSize = totalData / currentBatchCount;
                return true;
            }
            return false;
        }
    }
}
