using System;
using System.Threading.Tasks;
using Orleans;
using Orleans.Concurrency;
using Orleans.CodeGeneration;
using Orleans.Transactions.Abstractions;
using Interfaces;
using Global;

[assembly: GenerateSerializer(typeof(Grains.BallAttr))]

namespace Grains
{
    [Serializable]
    public class BallAttr
    {
        public uint Id { get; set; }
        public double Px { get; set; }
        public double Py { get; set; } 
        public double Vx { get; set; } 
        public double Vy { get; set; } 

    }

    [Reentrant]
    public class BallGrain : Grain, IBallGrain
    {
        private readonly ITransactionalState<BallAttr> attr;

        public BallGrain(
            [TransactionalState("attr")] ITransactionalState<BallAttr> attr)
        {
            this.attr = attr ?? throw new ArgumentNullException(nameof(attr));
        }

        double GetInitV()
        {
            Random rnd = new Random();
            if (rnd.NextDouble() > 0.5)
            {
                return 2;
            }
            return -2;
        }
        Task IBallGrain.Init(double px, double py)
        {
            long key = this.GetPrimaryKeyLong();
            uint ball_id = (uint)key;
            Console.WriteLine($"\n\nball {ball_id} init----- px = {px} py = {py}\n\n");
            return this.attr.PerformUpdate(x =>
            {
                x.Id = ball_id;
                x.Px = px;
                x.Py = py;
                x.Vx = GetInitV();
                x.Vy = GetInitV();
            });
        }

        Task IBallGrain.Move()
        {
            uint ball_id = attr.PerformRead(x => x.Id).Result;
            
            attr.PerformUpdate(x => {
                x.Px += x.Vx;
                x.Py += x.Vy;
            });

            double px = attr.PerformRead(x => x.Px).Result;
            if (px < 0)
            {
                attr.PerformUpdate(x => {
                    x.Px = 0;
                    x.Vx = -x.Vx;
                });
            }
            if (px > Common.x_max)
            {
                attr.PerformUpdate(x => {
                    x.Px = Common.x_max;
                    x.Vx = -x.Vx;
                });
            }
            double py = attr.PerformRead(x => x.Py).Result;
            if (py < 0)
            {
                attr.PerformUpdate(x => {
                    x.Py = 0;
                    x.Vy = -x.Vy;
                });
            }
            if (py > Common.y_max)
            {
                attr.PerformUpdate(x => {
                    x.Py = Common.y_max;
                    x.Vy = -x.Vy;
                });
            }

            Console.WriteLine($"\n\nball {ball_id} move px = {px} py = {py}\n\n");

            return Task.CompletedTask;
        }

        Task<long> IBallGrain.GetInGridId()
        {
            double px = attr.PerformRead(x => x.Px).Result;
            double py = attr.PerformRead(x => x.Py).Result;

            uint x_grid_index = (uint)px / Common.grid_len;
            if (x_grid_index >= Common.x_grid_num)
            {
                x_grid_index = Common.x_grid_num - 1;
            }

            uint y_grid_index = (uint)py / Common.grid_wid;
            if (y_grid_index >= Common.y_grid_num)
            {
                y_grid_index = Common.y_grid_num - 1;
            }

            long grid_id = Common.GetGridId(x_grid_index, y_grid_index);
            return Task.FromResult(grid_id);
        }
    }
}
