using System;
using System.Threading.Tasks;
using Orleans;
using Orleans.CodeGeneration;
using Orleans.Transactions.Abstractions;
using Orleans.Concurrency;
using Interfaces;
using System.Collections.Generic;
using System.Linq;
using Global;


[assembly: GenerateSerializer(typeof(Grains.BallArray))]

namespace Grains
{

    public class GridAttr
    {
        public uint Id { get; set; }
        public uint Px { get; set; }
        public uint Py { get; set; }
    }

    [Serializable]
    public class BallArray
    {
        public long[] Value = { };  
        public void Add(long v)
        {
            int len = Value.Length;
            Array.Resize(ref Value, len + 1);
            Value[len] = v;
        }

        public void remove(long v)
        {
            int len = Value.Length;
            for (int i = 0; i < len; i++)
            {
                if (Value[i] == v)
                {
                    Value[i] = Value[len - 1];
                    Array.Resize(ref Value, len - 1);
                    return;
                }
            }
        }
    }
	
	[Reentrant]
    public class GridGrain : Grain, IGridGrain
    {
        GridAttr attr = new GridAttr();
        private readonly ITransactionalState<BallArray> ball_arr;

        public GridGrain(
            [TransactionalState("ball_array")]
            ITransactionalState<BallArray> ball_arr
            )
        {
            this.ball_arr = ball_arr ?? throw new ArgumentNullException(nameof(ball_arr));
        }

        List<long> InnerGetBallList()
        {
            long[] balls = ball_arr.PerformRead(x => x.Value).Result;
            return balls.ToList();
        }

        double GetBallInitPx()
        {
            double px_low = this.attr.Px + Common.ball_radius;
            double px_high = this.attr.Px + Common.grid_len - Common.ball_radius;

            Random rnd = new Random();
            double init_px = px_low + (px_high - px_low) * rnd.NextDouble();
            return init_px;
        }

        double GetBallInitPy()
        {
            double py_low = this.attr.Py + Common.ball_radius;
            double py_high = this.attr.Py + Common.grid_wid - Common.ball_radius;

            Random rnd = new Random();
            double init_py = py_low + (py_high - py_low) * rnd.NextDouble();
            return init_py;
        }

        Task IGridGrain.Init()
        {
            long key = this.GetPrimaryKeyLong();
            uint grid_id = (uint)key;

            uint x_grid_index = (grid_id - 1) / Common.y_grid_num;
            uint y_grid_index = (grid_id -1) % Common.y_grid_num;

            uint grid_px = x_grid_index * Common.grid_len;
            uint grid_py = y_grid_index * Common.grid_wid;

            this.attr.Id = grid_id;
            this.attr.Px = grid_px;
            this.attr.Py = grid_py;

            for(long i = 0; i < Common.per_ball_count; i++)
            {
                ball_arr.PerformUpdate(x => x.Add(10000 * grid_id + i));
            }

            var tasks = new List<Task>();
            List<long> ball_list = InnerGetBallList();
            foreach (var ball_id in ball_list)
            {
                tasks.Add(GrainFactory.GetGrain<IBallGrain>(ball_id).Init(GetBallInitPx(), GetBallInitPy()));
            }
            return Task.WhenAll(tasks);
        }

        async Task IGridGrain.Move()
        {
            var tasks = new List<Task>();
            List<long> ball_list = InnerGetBallList();
			int ball_count = ball_list.Count;
            if (ball_count > 2)
            {
                Console.WriteLine($"\n\ngrid_id = {this.attr.Id} ball_count = {ball_count}\n\n");
            }
            foreach (var ball_id in ball_list)
            {
                tasks.Add(GrainFactory.GetGrain<IBallGrain>(ball_id).Move());
				Console.WriteLine($"\n\ngrid_id = {this.attr.Id} move ball id = {ball_id}\n\n");
            }
            await Task.WhenAll(tasks);
			
			tasks.Clear();
            foreach (var ball_id in ball_list)
            {
                long in_grid_id = await GrainFactory.GetGrain<IBallGrain>(ball_id).GetInGridId();
                if (this.attr.Id != in_grid_id)
                {
                    tasks.Add(ball_arr.PerformUpdate(x => x.remove(ball_id)));
                    tasks.Add(GrainFactory.GetGrain<IGridGrain>(in_grid_id).AddBall(ball_id));
                }
            }
			await Task.WhenAll(tasks);

            return;
        }
        
        Task IGridGrain.AddBall(long ball_id)
        {
            return ball_arr.PerformUpdate(x => x.Add(ball_id));
        }
    }
}
