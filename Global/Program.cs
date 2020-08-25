using System;

namespace Global
{
    public static class Common 
    {
        public readonly static double ball_radius = 1;

		public readonly static uint per_ball_count = 2;

        public readonly static uint x_grid_num = 4;
        public readonly static uint y_grid_num = 4;
        public readonly static uint total_grid_num = x_grid_num * y_grid_num;

        public readonly static uint grid_len = 200;
        public readonly static uint grid_wid = 200;
        public readonly static uint x_max = x_grid_num * grid_len;
        public readonly static uint y_max = y_grid_num * grid_wid;

        public static long GetGridId(uint x_grid_index, uint y_grid_index)
        {
            return x_grid_index * y_grid_num + y_grid_index + 1;
        }
    }
}
