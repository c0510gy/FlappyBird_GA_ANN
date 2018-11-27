using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleGame_ANN
{
    class GA_ANN
    {
        //INPUT 값 : 1. 다음 장애물과의 거리, 2. 다음 장애물 입구 높이와의 차이, 3. 주인공의 y좌표 값.
        //각 레이어 퍼셉트론 수 : 3, 5, 2, 1
        //OUTPUT 값 : 1. Jump 여부.

        //DNA : 각 노드의 weight, bias 값
        Random rnd = new Random((int)DateTime.Now.Ticks);

        public double[,] DNAs;
        public int[] score;
        public int Best_Score = 0;
        public int Best_index;
        public int population = 20; //DNA개수
        public int DNA_cnt; //DNA 염기 개수

        ANN ann; //각 DNA를 계산하기 위한 ANN
        private int input_cnt = 3, output_cnt = 1; //Input, Output 레이어의 퍼셉트론 개수
        private int hidden_cnt = 2; //hidden layer 개수
        private int[] hidden; //각 hidden layer의 퍼셉트론 개수

        private int gen = 0;

        public GA_ANN()
        {
            //ANN 설정
            hidden = new int[hidden_cnt];
            hidden[0] = 5;
            hidden[1] = 2;

            gen = 0;

            //GA 설정
            //20 + 12 + 3 = 35
            DNA_cnt = 35;
            DNAs = new double[population, DNA_cnt];
            for(int j = 0; j < population; j++)
            {
                for(int c = 0; c < DNA_cnt; c++)
                {
                    DNAs[j, c] = rnd.NextDouble() * 2 - 1;
                }
            }
            score = new int[population];
        }

        public int DNASimul(int j, IntPtr handler) //DNA 구동
        {
            SimpleGame sg;
            ANN t_ann = new ANN(input_cnt, hidden_cnt, output_cnt, hidden);
            int DNA_idx = 0;
            string dna_str = "";
            for (int l = 1; l <= hidden_cnt + 1; l++)
            {
                for (int m = 0; m < t_ann.count_P[l]; m++)
                {
                    for (int f = 0; f < t_ann.count_P[l - 1]; f++)
                    {
                        t_ann.P[l, m, f].w = DNAs[j, DNA_idx];
                        dna_str += DNAs[j, DNA_idx] + ",";
                        DNA_idx++;
                    }
                    t_ann.bias[l, m] = DNAs[j, DNA_idx];
                    dna_str += DNAs[j, DNA_idx] + ",";
                    DNA_idx++;
                }

            }

            string path = @"datas\";
            
            System.IO.Directory.CreateDirectory(path + gen + "_best"); //저장
            System.IO.File.WriteAllText(path + gen + "_best" + @"\info.txt", j + " : " + dna_str);

            int frameN = 0;
            sg = new SimpleGame(800, 400);
            while (true)
            {
                sg.NextFrame();
                sg.DrawFrame(); //Frame 그리기

                //판단을 위한 ANN에 조건 삽입
                t_ann.Per[0, 0].v = sg.nearXDis;
                t_ann.Per[0, 1].v = sg.nearYDis;
                t_ann.Per[0, 2].v = sg.rnr.y;
                //t_ann.Per[0, 3].v = sg.height - sg.rnr.y;
                //다음 행동 계산
                double[] solved = t_ann.solve();
                t_ann.DrawThings(false); //ANN 시각화
                if (solved[0] >= 0.5)
                {
                    sg.rnr.Jump();
                }

                //게임 화면과 ANN시각화 화면 기록
                Bitmap tmp = new Bitmap(sg.visual.Width + t_ann.visual.Width + 100, sg.visual.Height > t_ann.visual.Height ? sg.visual.Height : t_ann.visual.Height);
                Graphics g = Graphics.FromImage(tmp);
                g.Clear(Color.White); // 배경 설정.
                g.DrawImage(sg.visual, 0, 0);
                g.DrawImage(t_ann.visual, sg.visual.Width + 100, 0);

                using (var graphics = Graphics.FromHwnd(handler))
                using (var image = (Image)(new Bitmap(tmp)))
                {
                    //graphics.Clear(Color.Black);
                    graphics.DrawImage(image, 50, 50, image.Width, image.Height);
                }

                tmp.Save(path + gen + "_best" + @"\" + frameN + ".png", System.Drawing.Imaging.ImageFormat.Png);
                frameN++;

                if (sg.GameOver)
                {
                    score[j] = sg.Score;
                    break;
                }
                //Thread.Sleep(33);
            }

            return score[j];
        }

        public void GetScores()
        {
            SimpleGame sg;
            ANN t_ann = new ANN(input_cnt, hidden_cnt, output_cnt, hidden);

            for(int j = 0; j < population; j++)
            {
                //해당 DNA정보 및 실행결과 저장을 위한 변수
                string strinfo = j + " : "; // <인구 index> : <DNA Info>
                for(int i = 0; i < DNA_cnt; i++)
                {
                    strinfo += DNAs[j, i] + ",";
                }
                //==============

                //DNA -> ANN Weight and ANN bias 값으로 변환
                //3 + 5 + 2 + 1
                int DNA_idx = 0;
                for(int l = 1; l <= hidden_cnt + 1; l++)
                {
                    for(int m = 0; m < t_ann.count_P[l]; m++)
                    {
                        for (int f = 0; f < t_ann.count_P[l - 1]; f++)
                        {
                            t_ann.P[l, m, f].w = DNAs[j, DNA_idx];
                            DNA_idx++;
                        }
                        t_ann.bias[l, m] = DNAs[j, DNA_idx];
                        DNA_idx++;
                    }
                    
                }

                string path = @"datas\" + gen + @"\";
                System.IO.Directory.CreateDirectory(path);
                System.IO.File.WriteAllText(path + "info_" + j + ".txt", strinfo);
                
                sg = new SimpleGame(800, 400);
                while (true)
                {
                    sg.NextFrame();

                    //판단을 위한 ANN에 조건 삽입
                    t_ann.Per[0, 0].v = sg.nearXDis; //가장 가까운 장애물과의 거리
                    t_ann.Per[0, 1].v = sg.nearYDis; //가장 가까운 장애물과의 y좌표 차
                    t_ann.Per[0, 2].v = sg.rnr.y; //y좌표
                                                  //t_ann.Per[0, 3].v = sg.height - sg.rnr.y;
                                                  //다음 행동 계산
                    double[] solved = t_ann.solve();

                    if (solved[0] >= 0.5)
                    {
                        sg.rnr.Jump();
                    }

                    if (sg.GameOver)
                    {
                        score[j] = sg.Score;
                        break;
                    }
                }

                System.IO.File.WriteAllText(path + "info_score_" + j + ".txt", score[j] + "");
            }
            
        }

        public void Mating()
        {
            gen++;

            GetScores();


            //Score 정렬
            int[] Indexer = new int[population];
            bool[] chk = new bool[population];
            for (int j = 0; j < population; j++)
                chk[j] = false;
            
            Best_Score = 0;
            for (int j = 0; j < population; j++)
            {
                int b_v = 0, b_i = 0;
                for(int u = 0; u < population; u++)
                {
                    if (!chk[u] && b_v <= score[u])
                    {
                        b_v = score[u];
                        b_i = u;
                    }
                }
                Indexer[j] = b_i;
                chk[b_i] = true;
            }
            Best_Score = score[Indexer[0]];
            Best_index = Indexer[0];

            //교배
            for(int j = 0; j < population / 2; j++)
            {
                int ti = population - 1 - j;
                
                for(int u = 0; u < DNA_cnt; u++)
                {
                    int tmp = rnd.Next(100);
                    if(tmp >= 95) //mutation
                    {
                        DNAs[Indexer[ti], u] = rnd.NextDouble() * 2 - 1;
                    }
                    else
                    {
                        tmp = rnd.Next(score[Indexer[j]] + score[Indexer[j + 1]]);
                        if(tmp < score[Indexer[j]])
                        {
                            DNAs[Indexer[ti], u] = DNAs[Indexer[j], u];
                        }
                        else
                        {
                            DNAs[Indexer[ti], u] = DNAs[Indexer[j + 1], u];
                        }
                    }
                }
            }
            

        }
    }

    class ANN
    {
        public struct PerceptronNet //각 레이어 사이 노드간 관계
        {
            public double w; //가중치
            public bool connect; //링크 확인
            public int colorCode; //색(기본값 0)
        }

        public struct Perceptron //각 퍼셉트론
        {
            public double i; //퍼셉트론 input
            public double v; //퍼셉트론 output
            public double d; //이상적 값
            public double error; //오차
            public int colorCode; //색(기본값 0)
        }
        int numberOfLayers = 5;

        public PerceptronNet[,,] P = new PerceptronNet[5, 100, 100]; //퍼셉트론 [layer, node, n] = Wn, 연결 체크
        public Perceptron[,] Per = new Perceptron[5, 100]; //각 퍼셉트론이 가지는 값
        public int[] count_P = new int[5]; //퍼셉트론의 개수
        public double[,] bias = new double[5, 100]; //aX + bias

        int[] ap1 = new int[2]; //활성화된 회로1 [layer, node]
        int[] ap2 = new int[2]; //활성화된 회로2

        int radius = 10;
        int xGap = 100, yGap = 70;
        int maxN = 0; //레이어 당 최대 퍼셉트론 개수

        int testN = 0, nowtestN = 0; //학습 데이터 양, 현재 학습한 양
        public bool learningnow = false; //현재 학습중인가?

        double learningRate = 1; //0.25;

        int cx = 0, cy = 0; //현재 카메라 위치

        public Bitmap visual; //ANN 시각화

        // 입력 퍼셉트론 수, 레이어 수, 출력 퍼셉트론 수, 각 레이어 퍼셉트론 수
        public ANN(int inputP, int layersN, int outputP, int[] layersP)
        {
            P = new PerceptronNet[numberOfLayers, 100, 100];
            Per = new Perceptron[numberOfLayers, 100];
            bias = new double[numberOfLayers, 100];

            int a1 = inputP, a2 = layersN, a3 = outputP;
            numberOfLayers = a2 + 2; //총 레이어 수
            P = new PerceptronNet[numberOfLayers, 100, 100];
            Per = new Perceptron[numberOfLayers, 100];
            count_P = new int[numberOfLayers];
            bias = new double[numberOfLayers, 100];

            count_P[0] = a1;

            //레이어당 퍼셉트론 개수 정보
            for (int j = 0; j < a2; j++)
            {
                count_P[1 + j] = layersP[j];
            }

            count_P[a2 + 1] = a3;

            var random = new Random((int)DateTime.Now.Ticks); //초기값
            //link 연결
            for (int j = 1; j < numberOfLayers; j++)
            {
                if (maxN < count_P[j]) maxN = count_P[j];
                for (int k = 0; k < count_P[j]; k++)
                {
                    for (int q = 0; q < count_P[j - 1]; q++)
                    {
                        if (random.Next(0, 2) == 0)
                        {
                            P[j, k, q].w = -random.NextDouble(); //랜덤 가중치
                        }
                        else
                        {
                            P[j, k, q].w = random.NextDouble(); //랜덤 가중치
                        }

                        P[j, k, q].connect = true;
                    }
                }
            }
        }

        public void DrawThings(bool draw, [Optional] IntPtr handler)
        {
            Font font = new Font("맑은 고딕", 16);
            Brush brush = new SolidBrush(Color.Black);

            visual = new Bitmap(xGap * (numberOfLayers + 1), yGap * (maxN + 1)); //ANN 시각화 Bitmap
            Graphics g = Graphics.FromImage(visual);
            g.Clear(Color.White); // 배경 설정.

            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            if (learningnow)
            {
                //e.Graphics.DrawString("SG 인공신경네트워크|" + "인공지능 자가 학습 중..", font, brush, 5, 25);
            }
            else
            {
                //e.Graphics.DrawString("SG 인공신경네트워크" + "", font, brush, 5, 25);
            }

            font = new Font("맑은 고딕", 8);
            brush = new SolidBrush(Color.DarkGray);

            for (int j = 0; j < numberOfLayers; j++)
            {
                for (int k = 0; k < count_P[j]; k++)
                {
                    int h = yGap * (maxN - count_P[j]) / 2; //퍼셉트론 위치를 중앙으로 맞추어 주기 위한 값.
                    Point a = new Point(xGap * (j + 1) - cx, h + yGap * (k + 1) - cy);

                    if (Per[j, k].colorCode == 0)
                    {
                        g.FillEllipse(brush, a.X - radius, a.Y - radius, radius + radius, radius + radius);
                    }
                    else if (Per[j, k].colorCode == 1)
                    {
                        brush = new SolidBrush(Color.LightBlue);
                        g.FillEllipse(brush, a.X - radius, a.Y - radius, radius + radius, radius + radius);
                        brush = new SolidBrush(Color.DarkGray);
                    }

                    g.DrawString("P" + j + k, font, new SolidBrush(Color.Black), a);
                    g.DrawString("" + String.Format("{0:0.00}", Math.Floor(Per[j, k].v * 100) / 100), font, new SolidBrush(Color.Black), xGap * (j + 1) - radius - cx, h + yGap * (k + 1) - radius - cy);
                }
            }

            Pen pen = new Pen(Color.Green, 2); //Pen 객체 생성
            pen.StartCap = LineCap.RoundAnchor; //Line의 시작점 모양 변경 
            pen.EndCap = LineCap.RoundAnchor; //Line의 끝점 모양 변경

            for (int j = 0; j < numberOfLayers - 1; j++)
            {
                for (int k = 0; k < count_P[j]; k++)
                {
                    for (int q = 0; q < count_P[j + 1]; q++)
                    {
                        if (P[j + 1, q, k].connect)
                        { //연결
                            int h1 = yGap * (maxN - count_P[j]) / 2;
                            int h2 = yGap * (maxN - count_P[j + 1]) / 2;
                            Point p1 = new Point(xGap * (j + 1) + radius - cx, h1 + yGap * (k + 1) - cy);
                            Point p2 = new Point(xGap * (j + 2) - radius - cx, h2 + yGap * (q + 1) - cy);

                            if (P[j + 1, q, k].colorCode == 0)
                            {
                                g.DrawLine(pen, p1, p2);
                            }
                            else if (P[j + 1, q, k].colorCode == 1)
                            {
                                pen = new Pen(Color.Blue, 2);
                                g.DrawLine(pen, p1, p2);
                                pen = new Pen(Color.Green, 2);
                            }
                        }
                    }
                }
            }

            //visual.RotateFlip(RotateFlipType.Rotate90FlipNone); //시계방향 90도 회전

            //brush = new SolidBrush(Color.Red);
            //e.Graphics.FillEllipse(brush, gole.X - radius, gole.Y - radius, radius + radius, radius + radius); //목적지

            //시각화
            if (draw == true)
            {
                using (var graphics = Graphics.FromHwnd(handler))
                using (var image = (Image)visual)
                    graphics.DrawImage(image, 50, 100, image.Width, image.Height);
            }
        }


        public double[] solve() //인공신경망 계산 => 결과 array리턴
        {
            double[] r = new double[count_P[numberOfLayers - 1]];
            for (int j = 1; j < numberOfLayers; j++)
            {
                for (int k = 0; k < count_P[j]; k++)
                {
                    double v = bias[j, k];
                    for (int q = 0; q < count_P[j - 1]; q++)
                    {
                        if (P[j, k, q].connect)
                        {
                            v += Per[j - 1, q].v * P[j, k, q].w;

                            ap2[0] = j; ap2[1] = k;
                            ap1[0] = j - 1; ap1[1] = q;
                            //Thread.Sleep(100);

                            if (Per[j - 1, q].v >= 0.5)
                            {
                                P[j, k, q].colorCode = 1;
                            }
                            else
                            {
                                P[j, k, q].colorCode = 0;
                            }
                        }
                    }
                    Per[j, k].i = v;
                    Per[j, k].v = fy(v);

                    if (Per[j, k].v >= 0.5)
                    {
                        //P[j, k, q].colorCode = 1;
                        Per[j, k].colorCode = 1;
                        //Per[j - 1, q].colorCode = 1;
                    }
                    else
                    {
                        //P[j, k, q].colorCode = 0;
                        Per[j, k].colorCode = 0;
                        //Per[j - 1, q].colorCode = 0;
                    }
                }
            }

            //계산 결과
            //string output = "", upperoutput = "";
            for (int j = 0; j < count_P[numberOfLayers - 1]; j++)
            {
                r[j] = Per[numberOfLayers - 1, j].v;
                //output += Per[numberOfLayers - 1, j].v.ToString() + (j == count_P[numberOfLayers - 1] - 1 ? "" : ",");
                //upperoutput += Math.Round(Per[numberOfLayers - 1, j].v).ToString() + (j == count_P[numberOfLayers - 1] - 1 ? "" : ",");
            }
            return r;
        }

        private double run() //학습 중 인공신경망 계산
        {
            for (int j = 1; j < numberOfLayers; j++)
            {
                for (int k = 0; k < count_P[j]; k++)
                {
                    double v = bias[j, k];
                    for (int q = 0; q < count_P[j - 1]; q++)
                    {
                        if (P[j, k, q].connect)
                        {
                            v += Per[j - 1, q].v * P[j, k, q].w;

                            ap2[0] = j; ap2[1] = k;
                            ap1[0] = j - 1; ap1[1] = q;
                            //Thread.Sleep(1);
                        }
                    }
                    Per[j, k].i = v;
                    Per[j, k].v = fy(v);
                }
            }

            // 계산 정확도 측정
            double ac = 0;
            for (int j = 0; j < count_P[numberOfLayers - 1]; j++)
            {
                ac += Math.Round(100 * (1 - (Math.Max(Per[numberOfLayers - 1, j].v, Per[numberOfLayers - 1, j].d) - Math.Min(Per[numberOfLayers - 1, j].v, Per[numberOfLayers - 1, j].d))), 2);
            }
            ac /= count_P[numberOfLayers - 1];

            return ac; //정확도 반환
        }

        public double MachineLearning(string data, bool draw, [Optional] IntPtr handler) //학습
        {
            learningnow = true;

            double acc = 0;

            string[] datas = data.Split('&');
            testN = datas.Count();
            nowtestN = 0;

            /*
            //학습 진행 상황 출력
            this.Invoke(new Action(delegate () {
                progressBar1.Value = nowtestN;
                progressBar1.Maximum = testN;
                label11.Text = "학습 진행 = " + Math.Round(100 * ((double)nowtestN / (double)testN), 2).ToString() + "% (" + testN + ")";
                //textBox4.Text = Math.Round(Per[3, 0].v).ToString();
            }));
            */

            for (int t = 0; t < testN; t++)
            {
                //set input value
                for (int j = 0; j < datas[t].Split(',').Count() - count_P[numberOfLayers - 1]; j++)
                {
                    Per[0, j].v = double.Parse(datas[t].Split(',')[j]);
                }
                //이상 수치 설정
                for (int j = 0; j < count_P[numberOfLayers - 1]; j++)
                {
                    Per[numberOfLayers - 1, j].d = double.Parse(datas[t].Split(',')[datas[t].Split(',').Count() - count_P[numberOfLayers - 1] + j]);
                }

                acc += run(); //가동

                //역전파
                for (int j = numberOfLayers - 1; j > 0; j--)
                {
                    for (int k = 0; k < count_P[j]; k++)
                    {
                        if (j == numberOfLayers - 1) Per[j, k].error = fprime(Per[j, k].v) * (Per[j, k].d - Per[j, k].v);
                        else
                        {
                            double Errorsum = 0; int cnt = 0;
                            for (int q = 0; q < count_P[j + 1]; q++)
                                if (P[j + 1, q, k].connect)
                                {
                                    Errorsum += Per[j + 1, q].error * P[j + 1, q, k].w;
                                    cnt++;
                                }
                            Per[j, k].error = fprime(Per[j, k].v) * Errorsum / cnt;
                        }
                        for (int q = 0; q < count_P[j - 1]; q++)
                        {
                            if (P[j, k, q].connect)
                            {
                                double delta = Per[j, k].error;//Per[j, k].error * Per[j - 1, q].v * P[j, k, q].w * Math.Exp(-Per[j, k].i) / Math.Pow(1 + Math.Exp(-Per[j, k].i), 2);
                                //if (Per[j, k].d != Math.Round(Per[j, k].v)) { }

                                //가중치 갱신. w = w - E*delta(dE/dw)

                                if (Per[j, k].v >= 0.5)
                                {
                                    P[j, k, q].colorCode = 1;
                                    Per[j, k].colorCode = 1;
                                    Per[j - 1, q].colorCode = 1;
                                }
                                else
                                {
                                    P[j, k, q].colorCode = 0;
                                    Per[j, k].colorCode = 0;
                                    Per[j - 1, q].colorCode = 0;
                                }

                                P[j, k, q].w += Per[j - 1, q].v * delta * learningRate; //learningRate //갱신

                                //j-1, q의 이상수치, 오차 값 계산 필요함.
                                //MessageBox.Show(delta + "");

                                bias[j, k] += delta;
                                Per[j - 1, q].d = Per[j, k].d;
                            }
                        }
                    }
                }
                nowtestN++;
                /*
                //진행상황 갱신
                this.Invoke(new Action(delegate () {
                    progressBar1.Value = nowtestN;
                    label11.Text = "학습 진행 = " + Math.Round(100 * ((double)nowtestN / (double)testN), 2).ToString() + "% (" + testN + ")";
                    //textBox4.Text = Math.Round(Per[3, 0].v).ToString();
                }));
                */

                if (draw == true)
                {
                    DrawThings(true, handler);
                }
            }

            acc /= testN;
            learningnow = false;
            return acc; //평균 정확도 반환
        }

        private double fprime(double x)
        {
            return x * (1 - x);
        }

        private double fy(double X) //Sigmoid function
        {
            return (1 / (1 + Math.Exp(-X)));
        }
    }
}