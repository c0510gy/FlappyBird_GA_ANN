using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleGame_ANN
{
    class SimpleGame
    {
        //주인공
        public class Runner
        {
            public float x, y; //위치
            public int radius = 10;
            public float g = 10; //중력 속도 (가속도 적용 안함)

            public float vY = 0; //y속도

            private float deltaT = 0.1f;

            public Runner(int x, int y)
            {
                this.x = x;
                this.y = y;
            }

            public void Gravity()
            {
                if (vY < 0)
                {
                    this.vY += g * deltaT;
                    this.y += vY;
                }
                else
                {
                    this.y += g;
                }

            }

            public void Jump()
            {
                vY = -10;
            }
        }
        //장애물
        public class Obstacle
        {
            public int x;
            public int y, h; //뚫린 공간
            public bool enable = false; //작동중

            public Obstacle(int x, int y, int h)
            {
                this.x = x;
                this.y = y;
                this.h = h;

                this.enable = true;
            }

            public Obstacle()
            {
                this.enable = false;
            }
        }

        public Bitmap visual; //Game 화면
        public int width, height; //게임 크기
        Obstacle[] obs;
        public Runner rnr;
        public int Score = 0;
        public bool GameOver = false;

        public int nearXDis;
        public int nearYDis;

        private int MAX_OBSTACLES = 3; //최대 장애물
        private int MIN_OBSTACLE_HEIGHT = 80; //장애물의 최소 공백 높이
        private int MAX_OBSTACLE_HEIGHT = 150; //장애물의 최대 공백 높이
        private int SPEED_OBSTACLES = 10; //장애물 속도
        private int WIDTH_OBSTACLES = 50; //장애물 가로 길이
        private int MAX_SCORE = 10000000; //최대 점수값

        Random rnd = new Random((int)DateTime.Now.Ticks);

        public SimpleGame(int width, int height)
        {
            this.width = width;
            this.height = height;
            this.GameOver = false;
            Score = 0;

            obs = new Obstacle[MAX_OBSTACLES];
            for (int j = 0; j < MAX_OBSTACLES; j++)
            {
                obs[j] = new Obstacle();
            }
            rnr = new Runner(50, height / 2);

            visual = new Bitmap(this.width, this.height);
        }

        public void NextFrame()
        {
            if (GameOver) return;

            int obs_cnt = 0;
            int Max_X = 0;
            //모든 장애물 이동
            for (int j = 0; j < MAX_OBSTACLES; j++)
            {
                if (obs[j].enable)
                {
                    obs[j].x -= SPEED_OBSTACLES;
                    if (obs[j].x + WIDTH_OBSTACLES < 0)
                    {
                        obs[j].enable = false;
                    }
                    else
                    {
                        obs_cnt++;

                        if (Max_X < obs[j].x + WIDTH_OBSTACLES)
                        {
                            Max_X = obs[j].x + WIDTH_OBSTACLES;
                        }
                    }
                }
            }

            //새로운 장애물
            if (Max_X < width - WIDTH_OBSTACLES * 5)
                if (obs_cnt <= MAX_OBSTACLES)
                {
                    int tmp = rnd.Next(100);
                    if (tmp <= 80)
                    {
                        for (int j = 0; j < MAX_OBSTACLES; j++)
                        {
                            if (!obs[j].enable)
                            {
                                obs[j] = new Obstacle(this.width, rnd.Next(MAX_OBSTACLE_HEIGHT, height - MAX_OBSTACLE_HEIGHT), rnd.Next(MIN_OBSTACLE_HEIGHT, MAX_OBSTACLE_HEIGHT));
                                break;
                            }
                        }
                    }
                }

            rnr.Gravity();

            nearXDis = width + WIDTH_OBSTACLES + 10; 
            //충돌 검사
            if (rnr.y + rnr.radius >= height || rnr.y - rnr.radius <= 0)
            {
                GameOver = true;
            }
            for (int j = 0; j < MAX_OBSTACLES; j++)
            {
                if (obs[j].enable)
                {
                    if(obs[j].x + WIDTH_OBSTACLES >= rnr.x)
                    {
                        int dis = (int)(obs[j].x + WIDTH_OBSTACLES - rnr.x);
                        if(dis < nearXDis)
                        {
                            nearXDis = dis;

                            nearYDis = (int)(obs[j].y + obs[j].h / 2 - rnr.y);
                        }
                    }
                    if (obs[j].x <= rnr.x + rnr.radius && obs[j].x + WIDTH_OBSTACLES >= rnr.x - rnr.radius)
                    {
                        if (!(obs[j].y < rnr.y - rnr.radius && obs[j].y + obs[j].h > rnr.y + rnr.radius))
                        {
                            //장애물과 충돌함!
                            GameOver = true;
                            break;
                        }
                    }
                }
            }

            Score++;
            if(Score > MAX_SCORE)
            {
                GameOver = true;
            }
        }

        public void DrawFrame()
        {
            //visual = new Bitmap(this.width, this.height);
            Graphics g = Graphics.FromImage(visual);
            g.Clear(Color.DarkSeaGreen); // 배경 설정.

            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            Brush brush = new SolidBrush(Color.DarkBlue);
            Font font = new Font("맑은 고딕", 15, FontStyle.Bold);

            for (int j = 0; j < MAX_OBSTACLES; j++)
            {
                if (obs[j].enable)
                {
                    g.FillRectangle(brush, obs[j].x, 0, WIDTH_OBSTACLES, obs[j].y);
                    g.FillRectangle(brush, obs[j].x, obs[j].y + obs[j].h, WIDTH_OBSTACLES, height - (obs[j].y + obs[j].h));
                }
            }

            brush = new SolidBrush(Color.Red);
            g.FillRectangle(brush, rnr.x - rnr.radius, rnr.y - rnr.radius, rnr.radius * 2, rnr.radius * 2);

            brush = new SolidBrush(Color.Black);
            g.DrawString("점수 " + Score + "", font, brush, width - 150, 10);
        }
    }
}
