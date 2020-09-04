using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Globalization;

namespace RyskanovDiplom
{   //****************************************************************
    // Структура для возвращения результата пересечения
    //****************************************************************
    public struct Resultintersection
    {
        public int status;
        public PointF point;
    }

    //****************************************************************
    // Класс Poligon - замкнутый многоугольник
    //****************************************************************
    public class Poligon
    {
        public const float delta = 0.000001F;            // Маленькая величина для сравнения точек и проверки деления на ноль
        private const float stepCorrect = 0.001F;        // Маленькая величина, применяется при определении нахождения точки внутри тела чтобы точка не совпадала с вершиной
        private const float махPoint = 10000F;           // Максимальное значение координат точки, чтобы работать с отрезками а не с прямыми,
        Matrix fMatrix = new Matrix();                   // Матрица преобразования фигуры
        public PointF[] arrVertex = new PointF[0];       // Спсисок вершин многоугольника
        public PointF[] arrVertexByPass = new PointF[0]; // Спсисок вершин многоугольника для обхода
        private const float deltaByPass = 0.1F;          // Отступ контура для обхода от реального контура фигуры
        public bool completed = false;                   // Фигура завершена



        //****************************************************************
        // Добавление вершины
        //****************************************************************
        public void AddVertex(PointF mPoint)
        {
            // Изменяем размер массива
            Array.Resize(ref arrVertex, arrVertex.Length + 1);
            // В последний элемент массива добавляем переданную точку
            arrVertex[arrVertex.Length - 1] = mPoint;
        }

        //****************************************************************
        // Добавление вершины
        //****************************************************************
        void AddVertex(float x, float y)
        {
            PointF p = new PointF(x, y);
            AddVertex(p);
        }

        //****************************************************************
        // Удаление вершины. n-Индекс удаляемой вершины
        //****************************************************************
        public void DeleteVertex(int n)
        {
            //Скопируем хвост массива
            for (int i = n; i < arrVertex.Length - 1; i++)
            {
                arrVertex[i] = arrVertex[i + 1];
            }
            //Изменяем размер массива
            Array.Resize(ref arrVertex, arrVertex.Length - 1);
        }

        //****************************************************************
        // Удаление всех вершины.
        //****************************************************************
        public void DeleteAllVertex()
        {
            //Изменяем размер массива
            Array.Resize(ref arrVertex, 0);
        }

        //****************************************************************
        // Вращение фигуры
        //****************************************************************
        public void Rotation(float gradus, PointF pRotate)
        {
            //Сбросили матрицу преобразования
            fMatrix.Reset();
            //Делаем вращение по часовой стрелке
            fMatrix.RotateAt(gradus, pRotate);
            //Применяем преобразование к нашему массиву точек
            fMatrix.TransformPoints(arrVertex);
        }

        //****************************************************************
        // Пермещение фигуры
        //****************************************************************
        public void Move(float xMove, float yMove)
        {
            //fMatrix.Reset();
            //fMatrix.Shear(xMove, yMove);
            //fMatrix.TransformPoints(arrVertex);
            //fMatrix.Reset();

            for (int i = 0; i < arrVertex.Length; i++)
            {
                arrVertex[i].X = arrVertex[i].X + xMove;
                arrVertex[i].Y = arrVertex[i].Y + yMove;
            }
        }

        //****************************************************************
        // Векторное произведение веторов а и ь
        //****************************************************************
        private static float vectorProduct(float aX, float aY, float bX, float bY)
        {
            return aX * bY - bX * aY;
        }

        //****************************************************************
        // Возвращает структуру пересечения двух отрезков
        // status: -1 ошибка
        //          0 нет пересечения
        //          1 пересечение в 1-ой точке
        //          2 пересечение в 1-ой точке, пересекаются концы отрезков 
        //          3 отрезки имеют множество общих точек
        // point.x координата x точки пересечения
        // point.у координата у точки пересечения
        //****************************************************************
        public static Resultintersection intersectionSegment(PointF p11, PointF p12, PointF p21, PointF p22)
        {
            // Инициализируем возвращаемое значение
            Resultintersection ret;
            ret.status = -1;
            ret.point = new PointF(0, 0);

            // Получим векторное произведение векторов
            float v11 = vectorProduct(p12.X - p11.X, p12.Y - p11.Y, p21.X - p11.X, p21.Y - p11.Y);
            float v12 = vectorProduct(p12.X - p11.X, p12.Y - p11.Y, p22.X - p11.X, p22.Y - p11.Y);
            float v21 = vectorProduct(p22.X - p21.X, p22.Y - p21.Y, p11.X - p21.X, p11.Y - p21.Y);
            float v22 = vectorProduct(p22.X - p21.X, p22.Y - p21.Y, p12.X - p21.X, p12.Y - p21.Y);

            // Если векторные произведения почти нулевые обнулим
            // заодно подсчитаем количество нулевых векторных произведений
            int status = 0;
            if (Math.Abs(v11) < delta)
            {
                v11 = 0;
                status++;
                ret.point.X = p21.X;
                ret.point.Y = p21.Y;
            }
            if (Math.Abs(v12) < delta)
            {
                v12 = 0;
                status++;
                ret.point.X = p22.X;
                ret.point.Y = p22.Y;
            }
            if (Math.Abs(v21) < delta)
            {
                v21 = 0;
                status++;
                ret.point.X = p11.X;
                ret.point.Y = p11.Y;
            }
            if (Math.Abs(v22) < delta)
            {
                v22 = 0;
                status++;
                ret.point.X = p12.X;
                ret.point.Y = p12.Y;
            }
            //Проанализируем пересечение отрезков
            switch (status)
            {
                //Отрезки принадлежат одной прямой
                case 4:
                    //Анализируем по х
                    if (Math.Abs(p12.X - p11.X) > Math.Abs(p12.Y - p11.Y))
                    {
                        // Второй отрезок является продолжением первого
                        if (Math.Abs(Math.Max(p11.X, p12.X) - Math.Min(p21.X, p22.X)) < delta)
                        {
                            ret.status = 2;
                            if (p11.X > p12.X)
                            {
                                ret.point.X = p11.X;
                                ret.point.Y = p11.Y;
                            }
                            else
                            {
                                ret.point.X = p12.X;
                                ret.point.Y = p12.Y;
                            }

                        }
                        // Первый отрезок является продолжением второго
                        if (ret.status == -1)
                        {
                            if (Math.Abs(Math.Max(p21.X, p22.X) - Math.Min(p11.X, p12.X)) < delta)
                            {
                                ret.status = 2;
                                if (p21.X > p22.X)
                                {
                                    ret.point.X = p21.X;
                                    ret.point.Y = p21.Y;
                                }
                                else
                                {
                                    ret.point.X = p22.X;
                                    ret.point.Y = p22.Y;
                                }
                            }
                        }
                        // Проверим имеютли отрезки общие точки
                        if (ret.status == -1)
                        {
                            ret.point.X = 0;
                            ret.point.Y = 0;
                            if (((Math.Min(p11.X, p12.X) <= p21.X) && (p21.X <= Math.Max(p11.X, p12.X))) ||
                               ((Math.Min(p11.X, p12.X) <= p22.X) && (p22.X <= Math.Max(p11.X, p12.X))) ||
                               ((Math.Min(p21.X, p22.X) <= p11.X) && (p11.X <= Math.Max(p21.X, p22.X))))
                            {
                                ret.status = 3;
                            }
                            else
                            {
                                ret.status = 0;
                            }
                        }

                    }
                    //Анализируем по у
                    else
                    {
                        // Второй отрезок является продолжением первого
                        if (Math.Abs(Math.Max(p11.Y, p12.Y) - Math.Min(p21.Y, p22.Y)) < delta)
                        {
                            ret.status = 2;
                            if (p11.Y > p12.Y)
                            {
                                ret.point.Y = p11.Y;
                                ret.point.Y = p11.Y;
                            }
                            else
                            {
                                ret.point.Y = p12.Y;
                                ret.point.Y = p12.Y;
                            }
                        }

                        // Первый отрезок является продолжением второго
                        if (ret.status == -1)
                        {
                            if (Math.Abs(Math.Max(p21.Y, p22.Y) - Math.Min(p11.Y, p12.Y)) < delta)
                            {
                                ret.status = 2;
                                if (p21.Y > p22.Y)
                                {
                                    ret.point.X = p21.X;
                                    ret.point.Y = p21.Y;
                                }
                                else
                                {
                                    ret.point.X = p22.X;
                                    ret.point.Y = p22.Y;
                                }
                            }
                        }
                        // Проверим имеютли отрезки общие точки
                        if (ret.status == -1)
                        {
                            ret.point.X = 0;
                            ret.point.Y = 0;
                            if (((Math.Min(p11.Y, p12.Y) <= p21.Y) && (p21.Y <= Math.Max(p11.Y, p12.Y))) ||
                               ((Math.Min(p11.Y, p12.Y) <= p22.Y) && (p22.Y <= Math.Max(p11.Y, p12.Y))) ||
                               ((Math.Min(p21.Y, p22.Y) <= p11.Y) && (p11.Y <= Math.Max(p21.Y, p22.Y))))
                            {
                                ret.status = 3;
                            }
                            else
                            {
                                ret.status = 0;
                            }
                        }
                    }
                    break;
                //Отрезки пересекаются концами но не пренадлежат одной прямой
                case 2:
                    ret.status = 2;
                    break;
                //Конец одного из отрезков находится на прямой, которому принадлежит другой отрезок
                case 1:
                    //Нужно анализировать второй отрезок
                    if ((v11 * v12) == 0)
                    {
                        //Анализируем по x
                        if (Math.Abs(p12.X - p11.X) > Math.Abs(p12.Y - p11.Y))
                        {
                            if ((Math.Min(p11.X, p12.X) <= ret.point.X) && (ret.point.X <= Math.Max(p11.X, p12.X)))
                            {
                                ret.status = 1;
                            }
                            else
                            {
                                ret.point.X = 0;
                                ret.point.Y = 0;
                                ret.status = 0;
                            }
                        }
                        //Анализируем по у
                        else
                        {
                            if ((Math.Min(p11.Y, p12.Y) <= ret.point.Y) && (ret.point.Y <= Math.Max(p11.Y, p12.Y)))
                            {
                                ret.status = 1;
                            }
                            else
                            {
                                ret.point.X = 0;
                                ret.point.Y = 0;
                                ret.status = 0;
                            }
                        }
                    }
                    //Нужно анализировать первый отрезок
                    else
                    {
                        //Анализируем по x
                        if (Math.Abs(p22.X - p21.X) > Math.Abs(p22.Y - p21.Y))
                        {
                            if ((Math.Min(p21.X, p22.X) <= ret.point.X) && (ret.point.X <= Math.Max(p21.X, p22.X)))
                            {
                                ret.status = 1;
                            }
                            else
                            {
                                ret.point.X = 0;
                                ret.point.Y = 0;
                                ret.status = 0;
                            }
                        }
                        //Анализируем по у
                        else
                        {
                            if ((Math.Min(p21.Y, p22.Y) <= ret.point.Y) && (ret.point.Y <= Math.Max(p21.Y, p22.Y)))
                            {
                                ret.status = 1;
                            }
                            else
                            {
                                ret.point.X = 0;
                                ret.point.Y = 0;
                                ret.status = 0;
                            }
                        }
                    }
                    break;
                //Отрезки пересекаются в одной точке, но она не является концом ни одного из отрезков
                case 0:
                    //Пересекается
                    if (((v11 * v12) < 0) && ((v21 * v22) < 0))
                    {
                        ret.point.X = p21.X + (p22.X - p21.X) * Math.Abs(v11) / Math.Abs(v12 - v11);
                        ret.point.Y = p21.Y + (p22.Y - p21.Y) * Math.Abs(v11) / Math.Abs(v12 - v11);
                        ret.status = 1;
                    }
                    //Не пересекается
                    else
                    {
                        ret.point.X = 0;
                        ret.point.Y = 0;
                        ret.status = 0;
                    }
                    break;
                //Такого не должно быть
                default:
                    ret.point.X = 0;
                    ret.point.Y = 0;
                    ret.status = -1;
                    break;
            }
            return ret;
        }

        //****************************************************************
        // Рисует многоугольник
        //****************************************************************
        public void Draw(Graphics g, Pen pen)
        {
            // Пройдемся по списку вершин
            if (arrVertex.Count() > 2)
            {
                for (int i = 0; i < arrVertex.Count(); i++)
                {
                    if (i == (arrVertex.Count() - 1))
                    {
                        g.DrawLine(pen, arrVertex[i], arrVertex[0]);
                    }
                    else
                    {
                        g.DrawLine(pen, arrVertex[i], arrVertex[i + 1]);
                    }
                }
            }
        }

        //****************************************************************
        // Проверяет корректность новой вершины p2 многоугольника,
        // если новое ребро корректно возвращает истину
        //****************************************************************
        public Boolean checkVertex(PointF p2)
        {
            Boolean ret = true;
            float S;
            int result;

            // Если количество вершин больше одного
            if (arrVertex.Length > 0)
            {
                for (int i = 0; i < arrVertex.Length - 1; i++)
                {
                    // Посмотрим пересечение
                    result = intersectionSegment(arrVertex[i], arrVertex[i + 1], arrVertex[arrVertex.Length - 1], p2).status;
                    // Это пересечение со смежной гранью (последней) должна иметь общую вершину
                    if (i == (arrVertex.Length - 2))
                    {
                        if (result != 2)
                        {
                            ret = false;
                            break;
                        }
                    }
                    else
                    {
                        //Если введенная вершина это первая вершина и смотрим первую грань то должна быть общая вершина
                        S = (float)Math.Sqrt(Math.Pow(p2.X - arrVertex[0].X, 2) + Math.Pow(p2.Y - arrVertex[0].Y, 2));

                        if ((S < delta) && (i == 0))
                        {
                            if (result != 2)
                            {
                                ret = false;
                                break;
                            }
                        }
                        else
                        {
                            // В остальных случаях не должны пересекаться
                            if (result != 0)
                            {
                                ret = false;
                                break;
                            }
                        }
                    }
                }
                //Новая вершина не должна быть равна последней вершине
                S = (float)Math.Sqrt(Math.Pow(p2.X - arrVertex[arrVertex.Length - 1].X, 2) + Math.Pow(p2.Y - arrVertex[arrVertex.Length - 1].Y, 2));
                if (S < delta)
                {
                    ret = false;
                }
            }
            return ret;
        }


        //****************************************************************
        // Проверяет пересечение двух полигонов
        //****************************************************************
        public static Boolean CheckPoligon(ref Poligon poligon1, ref Poligon poligon2)
        {
            Boolean ret = true;
            PointF p11;
            PointF p12;
            PointF p21;
            PointF p22;
            int result;

            // Пройдемся по списку вершин
            if ((poligon1.arrVertex.Length > 2) && (poligon2.arrVertex.Length > 2))
            {
                for (int i1 = 0; i1 < poligon1.arrVertex.Length; i1++)
                {
                    for (int i2 = 0; i2 < poligon2.arrVertex.Length; i2++)
                    {
                        //Выберем отрезок первой фигуры
                        p11 = poligon1.arrVertex[i1];
                        if (i1 == (poligon1.arrVertex.Length - 1))
                        {
                            p12 = poligon1.arrVertex[0];
                        }
                        else
                        {
                            p12 = poligon1.arrVertex[i1 + 1];
                        }
                        //Выберем отрезок второй фигуры
                        p21 = poligon2.arrVertex[i2];
                        if (i2 == (poligon2.arrVertex.Length - 1))
                        {
                            p22 = poligon2.arrVertex[0];
                        }
                        else
                        {
                            p22 = poligon2.arrVertex[i2 + 1];
                        }
                        result = intersectionSegment(p11, p12, p21, p22).status;
                        //Если пересекаются то вернем ложь
                        if (result != 0)
                        {
                            ret = false;
                            break;
                        }
                    }
                }
            }
            return ret;
        }

        //****************************************************************
        // Находит проекцию точки на отрезок
        //****************************************************************
        public static PointF getDistance(PointF p1, PointF p2, PointF p0)
        {
            PointF ret = new PointF(0, 0);

            float K1;
            float K2;
            float B1;
            float B2;
            float delta = 5 * deltaByPass;

            //Найдем проекцию точки на прямую
            //Отрезок параллелен оси OX
            if ((p2.X - p1.X) == 0)
            {
                ret.X = p1.X;
                ret.Y = p0.Y;
            }
            else
            {
                //Отрезок параллелен оси OY
                if ((p2.Y - p1.Y) == 0)
                {
                    ret.X = p0.X;
                    ret.Y = p1.Y;
                }
                else
                {
                    //Общий случай, рассчитаем коэффициенты
                    K1 = (p2.Y - p1.Y) / (p2.X - p1.X);
                    B1 = p2.Y - K1 * p2.X;
                    K2 = -1 / K1;
                    B2 = p0.Y - K2 * p0.X;
                    ret.X = (B2 - B1) / (K1 - K2);
                    ret.Y = K1 * ret.X + B1;
                }
            }

            //Проверим точка лежит ли на отрезке
            bool inLine = false;
            if (p2.X > p1.X)
            {
                if (((p1.X - delta) <= ret.X) && (ret.X <= (p2.X + delta)))
                {
                    inLine = true;
                }
            }
            else
            {
                if (((p2.X - delta) <= ret.X) && (ret.X <= (p1.X + delta)))
                {
                    inLine = true;
                }
            }
            if (inLine)
            {
                float l = (float)Math.Sqrt(Math.Pow(p0.X - ret.X, 2) + Math.Pow(p0.Y - ret.Y, 2));
                if (l > delta)
                {
                    ret.X = float.MinValue;
                    ret.Y = float.MinValue;
                }
            }
            else
            {
                ret.X = float.MinValue;
                ret.Y = float.MinValue;
            }
            return ret;
        }


        //****************************************************************
        // Объединение двух полигонов вспомогательная
        //****************************************************************
        private static int Union(ref PointF[] arr1, int maxIndex1, ref PointF[] arr2, int maxIndex2, ref Poligon poligonRez)
        {
            bool stop1;
            bool stop2;
            int i1;
            int i2;
            PointF V1;
            PointF V2;
            PointF V3;
            PointF V4;
            PointF V;
            int ret = -1;

            //По первой фигуре идем прямо, начиная с найденной вершины
            i1 = maxIndex1;
            stop1 = false;
            while (!stop1)
            {
                stop2 = false;
                i2 = maxIndex2;
                //Запомним концы отрезков
                V1 = arr1[i1];
                if (i1 == (arr1.Length - 1))
                {
                    V2 = arr1[0];
                }
                else
                {
                    V2 = arr1[i1 + 1];
                }

                //По второй фигуре идем в обратную сторону, начиная с найденной вершины
                while (!stop2)
                {
                    //Запомним концы отрезков
                    V3 = arr2[i2];
                    if (i2 == 0)
                    {
                        V4 = arr2[arr2.Length - 1];
                    }
                    else
                    {
                        V4 = arr2[i2 - 1];
                    }
                    //Найдем точку на отрезке V1, V2
                    V = getDistance(V1, V2, V4);
                    if (V.X > float.MinValue)
                    {
                        //Если нашли точку касания, то допишем вершины из второго полигона
                        poligonRez.AddVertex(V1);
                        //poligonRez.AddVertex(V);
                        poligonRez.AddVertex(V4);
                        int i3 = i2;
                        while (i3 != maxIndex2)
                        {
                            poligonRez.AddVertex(arr2[i3]);
                            i3++;
                            if (i3 == arr2.Length)
                            {
                                i3 = 0;
                            }
                        }
                        stop1 = true;
                        stop2 = true;
                        ret = 0;
                    }
                    else
                    {
                        //Если не нашли то ищем точку на отрезке V3, V4
                        V = getDistance(V3, V4, V2);
                        if (V.X > float.MinValue)
                        {
                            //Если нашли точку касания, то допишем вершины из второго полигона
                            poligonRez.AddVertex(V1);
                            poligonRez.AddVertex(V2);
                            //poligonRez.AddVertex(V);
                            int i3 = i2;
                            while (i3 != maxIndex2)
                            {
                                poligonRez.AddVertex(arr2[i3]);
                                i3++;
                                if (i3 == arr2.Length)
                                {
                                    i3 = 0;
                                }
                            }
                            stop1 = true;
                            stop2 = true;
                            ret = 0;
                        }
                    }

                    //Найдем следующий индекс i2
                    i2--;
                    if (i2 == -1)
                    {
                        i2 = arr2.Length - 1;
                    }
                    if (i2 == maxIndex2)
                    {
                        stop2 = true;
                    }
                }

                //Если не остановились, то запишем вершину в новый полигон
                if (!stop1)
                {
                    poligonRez.AddVertex(V1);
                }

                //Найдем следующий индекс i1
                i1++;
                if (i1 == arr1.Length)
                {
                    i1 = 0;
                }
                if (i1 == maxIndex1)
                {
                    stop1 = true;
                }
            }
            return ret;
        }

        //****************************************************************
        // Объединение двух полигонов основная
        //****************************************************************
        public static Poligon UnionPoligon(ref PointF[] arr1, ref PointF[] arr2)
        {
            Poligon ret = new Poligon();
            float maxLong = float.MinValue;
            int maxIndex1 = -1;
            int maxIndex2 = -1;
            float curLong = 0;

            // Найдем точки в двух полигонах максимально удаленных друг от друга
            for (int i1 = 0; i1 < arr1.Length; i1++)
            {
                for (int i2 = 0; i2 < arr2.Length; i2++)
                {
                    curLong = (float)Math.Sqrt(Math.Pow(arr1[i1].X - arr2[i2].X, 2) + Math.Pow(arr1[i1].Y - arr2[i2].Y, 2));
                    if (curLong > maxLong)
                    {
                        maxLong = curLong;
                        maxIndex1 = i1;
                        maxIndex2 = i2;
                    }
                }
            }

            // Объединим первую половину
            if (Union(ref arr1, maxIndex1, ref arr2, maxIndex2, ref ret) == 0)
            {
                // Объединим вторую половину
                Union(ref arr2, maxIndex2, ref arr1, maxIndex1, ref ret);
            }
            return ret;
        }

        //****************************************************************
        // Изменяем координату Y у точки, чтобы не совпадала c вершинами
        //****************************************************************
        private void Correction(ref PointF P)
        {
            bool stop = false;
            while (!stop)
            {
                stop = true;
                for (int i = 0; i < arrVertex.Length; i++)
                {
                    if (arrVertex[i].Y == P.Y)
                    {
                        P.Y = P.Y + stepCorrect;
                        stop = false;
                        break;
                    }
                }
            }
        }

        //****************************************************************
        // Проверяет принадлежность точки многоугольнику
        //****************************************************************
        public bool Belongs(PointF p)
        {
            Boolean ret = false;
            PointF p1;
            PointF p2;
            Resultintersection result;
            int counter = 0;

            // Скоректируем координаты точки, чтобы не совпадал с вершинами 
            Correction(ref p);
            p1 = new PointF(-махPoint, p.Y);
            p2 = new PointF(махPoint, p.Y);

            //Пройдемся по ребрам
            for (int i = 0; i < arrVertex.Length; i++)
            {
                if (i == (arrVertex.Length - 1))
                {
                    //Последняя грань
                    result = intersectionSegment(arrVertex[i], arrVertex[0], p1, p2);
                }
                else
                {
                    result = intersectionSegment(arrVertex[i], arrVertex[i + 1], p1, p2);
                }
                if (result.status == 1)
                {
                    if (result.point.X == p.X)
                    {
                        ret = true;
                    }
                    else
                    {
                        if (result.point.X > p.X)
                        {
                            counter++;
                        }
                    }
                }
            }
            //Посчитам четное или нечетное кол-во пересечений
            if (!ret)
            {
                if ((counter > 0) && ((counter % 2) == 1))
                {
                    ret = true;
                }
            }
            return ret;
        }

        //****************************************************************
        // Формирует контур для обхода
        //****************************************************************
        public void CreateVertexByPass()
        {
            float x = 0;                 //X координата анализируемой точки
            float y = 0;                 //Y координата анализируемой точки
            float x1 = 0;                //X координата 1-ой точки
            float y1 = 0;                //Y координата 1-ой точки
            float x2 = 0;                //X координата 2-ой точки
            float y2 = 0;                //Y координата 2-ой точки
            float xM1 = 0;               //X координата середины ребра
            float yM1 = 0;               //Y координата середины ребра
            float xM2 = 0;               //X координата середины ребра
            float yM2 = 0;               //Y координата середины ребра
            float k = 0;                 //Коэффициент перпендикуляра
            float d1X = 0;               //Приращение x
            float d1Y = 0;               //Приращение y
            float d2X = 0;               //Приращение x
            float d2Y = 0;               //Приращение y
            float xNew1 = 0;             //Итоговая точка 1
            float yNew1 = 0;             //Итоговая точка 1
            float xNew2 = 0;             //Итоговая точка 2
            float yNew2 = 0;             //Итоговая точка 2

            // Обнулим массив вершин для обхода
            arrVertexByPass = new PointF[0];
            // Если вершин меньше 3, то уходим
            if (arrVertex.Length < 3)
            {
                return;
            }
            // Пройдемся по вершинам
            for (int i = 0; i < arrVertex.Length; i++)
            {
                //Это первая вершина
                if (i == 0)
                {
                    x1 = arrVertex[arrVertex.Length - 1].X;
                    y1 = arrVertex[arrVertex.Length - 1].Y;
                }
                else
                {
                    x1 = arrVertex[i - 1].X;
                    y1 = arrVertex[i - 1].Y;
                }
                //Это последняя вершина
                if (i == arrVertex.Length - 1)
                {
                    x2 = arrVertex[0].X;
                    y2 = arrVertex[0].Y;
                }
                else
                {
                    x2 = arrVertex[i + 1].X;
                    y2 = arrVertex[i + 1].Y;
                }
                //Текущая вершина
                x = arrVertex[i].X;
                y = arrVertex[i].Y;
                //Вычислим первую точку ******************************************
                //Найдем середину ребра
                xM1 = (x1 + x) / 2;
                yM1 = (y1 + y) / 2;
                //Вычисли коэфициент перпендикуляра и прирощения для нахождении точки
                if (Math.Abs(y1 - y) > delta)
                {
                    k = -(x1 - x) / (y1 - y);
                    d1X = deltaByPass * (float)Math.Sqrt(1 / (1 + k * k));
                    d1Y = d1X * k;
                    d2X = -deltaByPass * (float)Math.Sqrt(1 / (1 + k * k));
                    d2Y = d2X * k;
                }
                else
                {
                    //Это перпендикуляр к горизонтальной прямой
                    d1X = 0;
                    d1Y = deltaByPass;
                    d2X = 0;
                    d2Y = -deltaByPass;
                }
                //Посмотрим которая из точек не принадлежит фигуре
                if (Belongs(new PointF(xM1 + d1X, yM1 + d1Y)))
                {
                    xNew1 = x + d2X;
                    yNew1 = y + d2Y;
                    xM1 = xM1 + d2X;
                    yM1 = yM1 + d2Y;
                }
                else
                {
                    xNew1 = x + d1X;
                    yNew1 = y + d1Y;
                    xM1 = xM1 + d1X;
                    yM1 = yM1 + d1Y;
                }
                //Вычислим вторую точку ******************************************
                //Найдем середину ребра
                xM2 = (x2 + x) / 2;
                yM2 = (y2 + y) / 2;
                //Вычисли коэфициент перпендикуляра и прирощения для нахождении точки
                if (Math.Abs(y2 - y) > delta)
                {
                    k = -(x2 - x) / (y2 - y);
                    d1X = deltaByPass * (float)Math.Sqrt(1 / (1 + k * k));
                    d1Y = d1X * k;
                    d2X = -deltaByPass * (float)Math.Sqrt(1 / (1 + k * k));
                    d2Y = d2X * k;
                }
                else
                {
                    //Это перпендикуляр к горизонтальной прямой
                    d1X = 0;
                    d1Y = deltaByPass;
                    d2X = 0;
                    d2Y = -deltaByPass;
                }
                //Посмотрим которая из точек не принадлежит фигуре
                if (Belongs(new PointF(xM2 + d1X, yM2 + d1Y)))
                {
                    xNew2 = x + d2X;
                    yNew2 = y + d2Y;
                    xM2 = xM2 + d2X;
                    yM2 = yM2 + d2Y;
                }
                else
                {
                    xNew2 = x + d1X;
                    yNew2 = y + d1Y;
                    xM2 = xM2 + d1X;
                    yM2 = yM2 + d1Y;
                }
                //Посмотрим итоговые точки на принадлежность фигуре, если хотябы одна из них 
                //принадлежит фигуре - этоа внутренний угол, то ищем пересечение
                if (Belongs(new PointF(xNew1, yNew1)) || Belongs(new PointF(xNew2, yNew2)))
                {
                    //Это внутренний угол, найдем пересечение отрезков
                    Resultintersection rs = intersectionSegment(new PointF(xNew1, yNew1), new PointF(xM1, yM1), new PointF(xNew2, yNew2), new PointF(xM2, yM2));
                    //Добавим вершину
                    Array.Resize(ref arrVertexByPass, arrVertexByPass.Length + 1);
                    arrVertexByPass[arrVertexByPass.Length - 1] = new PointF(rs.point.X, rs.point.Y);
                }
                else
                {
                    //Это внешний угол, нужна еще точка, всего три
                    xM1 = (xNew1 + xNew2) / 2;
                    yM1 = (yNew1 + yNew2) / 2;
                    d1X = xM1 - x;
                    d1Y = yM1 - y;
                    k = (float)Math.Sqrt(d1X * d1X + d1Y * d1Y);
                    if (Math.Abs(k) > delta)
                    {
                        //Общий случай
                        k = deltaByPass / k;
                        x1 = x + d1X * k;
                        y1 = y + d1Y * k;
                    }
                    else
                    {
                        //Это вертикаль
                        x1 = x;
                        y1 = y + deltaByPass;
                    }
                    //Добавим вершины
                    // Если расстояние между точками небольшое, то сделаем одну точку иначе три точки
                    if (Math.Sqrt(Math.Pow(xNew2 - xNew1, 2) + Math.Pow(yNew2 - yNew1, 2)) <= stepCorrect)
                    {
                        Array.Resize(ref arrVertexByPass, arrVertexByPass.Length + 1);
                        arrVertexByPass[arrVertexByPass.Length - 1] = new PointF(x1, y1);
                    }
                    else
                    {
                        Array.Resize(ref arrVertexByPass, arrVertexByPass.Length + 3);
                        arrVertexByPass[arrVertexByPass.Length - 3] = new PointF(xNew1, yNew1);
                        arrVertexByPass[arrVertexByPass.Length - 2] = new PointF(x1, y1);
                        arrVertexByPass[arrVertexByPass.Length - 1] = new PointF(xNew2, yNew2);
                    }
                }
            }
        }

        //****************************************************************
        // Проверяет возможность отрезать уши
        // arr - массив вершин, int v1,v2 - индексы соединяемых вершин
        // применяется при подсчете площади
        //****************************************************************
        private bool CheckLine(ref PointF[] arr, int v1, int v2)
        {
            Boolean ret = false;
            int v3;
            int v4;
            PointF p = new PointF();
            PointF p1 = new PointF(-махPoint, p.Y);
            PointF p2 = new PointF(махPoint, p.Y);
            Resultintersection result;
            int counter = 0;

            //Вершины которые не нужно проверять
            if (v1 == 0)
            {
                v3 = arr.Length - 1;
            }
            else
            {
                v3 = v1 - 1;
            }
            if (v2 == 0)
            {
                v4 = arr.Length - 1;
            }
            else
            {
                v4 = v2 - 1;
            }
            //Проверка на пересечение с другими гранями
            for (int i = 0; i < arr.Length; i++)
            {
                if ((i != v1) && (i != v2) && (i != v3) && (i != v4))
                {
                    if (i == (arr.Length - 1))
                    {
                        //Последняя грань
                        result = intersectionSegment(arr[i], arr[0], arr[v1], arr[v2]);
                    }
                    else
                    {
                        result = intersectionSegment(arr[i], arr[i + 1], arr[v1], arr[v2]);
                    }
                    if (result.status != 0)
                    {
                        return false;
                    }
                }
            }
            //Найдем центр отрезка и проверим на принадлежность фигуре
            p.X = (arr[v1].X + arr[v2].X) / 2;
            p.Y = (arr[v1].Y + arr[v2].Y) / 2;
            // Скоректируем координаты точки, чтобы не совпадал с вершинами 
            Correction(ref p);
            //Настроим прямую
            p1 = new PointF(-махPoint, p.Y);
            p2 = new PointF(махPoint, p.Y);
            //Пройдемся по ребрам
            for (int i = 0; i < arr.Length; i++)
            {
                if (i == (arr.Length - 1))
                {
                    //Последняя грань
                    result = intersectionSegment(arr[i], arr[0], p1, p2);
                }
                else
                {
                    result = intersectionSegment(arr[i], arr[i + 1], p1, p2);
                }
                if (result.status == 1)
                {
                    if (result.point.X == p.X)
                    {
                        ret = true;
                        break;
                    }
                    else
                    {
                        //Считаем количество пересечений
                        if (result.point.X > p.X)
                        {
                            counter++;
                        }
                    }
                }
            }
            if (!ret)
            {
                if ((counter > 0) && ((counter % 2) == 1))
                {
                    ret = true;
                }
            }
            return ret;
        }

        //****************************************************************
        //  Вычислить площадь триугольника
        //****************************************************************
        private float AreaTriangle(PointF p1, PointF p2, PointF p3)
        {
            return Math.Abs((p1.X - p3.X) * (p2.Y - p3.Y) - (p1.Y - p3.Y) * (p2.X - p3.X)) * 0.5F;
        }

        //****************************************************************
        //  Вычислить площадь фигуры
        //****************************************************************
        public float Area()
        {
            PointF[] vertexs = new PointF[0]; // Спсисок вершин многоугольника для обхода
            float area = 0;
            PointF p1 = new PointF();
            PointF p2 = new PointF();
            PointF p3 = new PointF();
            int i2 = 0;
            int i3 = 0;

            // Зададим размер
            Array.Resize(ref vertexs, arrVertex.Length);
            // Скопируем вершины
            Array.Copy(arrVertex, vertexs, arrVertex.Length);
            // Пройдемся по вершинам
            int cut = 0;
            int i = 0;
            while (vertexs.Length > 3)
            {
                i2 = i + 1;
                i3 = i + 2;
                //Выберем триугольник
                if (i == vertexs.Length - 2)
                {
                    i3 = 0;
                }

                if (i == vertexs.Length - 1)
                {
                    i2 = 0;
                    i3 = 1;
                }
                p1.X = vertexs[i].X;
                p1.Y = vertexs[i].Y;
                p2.X = vertexs[i2].X;
                p2.Y = vertexs[i2].Y;
                p3.X = vertexs[i3].X;
                p3.Y = vertexs[i3].Y;
                //Этот триугольник является ухом?
                if (CheckLine(ref vertexs, i, i3))
                {
                    //Это ухо. Посчитаем площадь.
                    area = area + AreaTriangle(p1, p2, p3);
                    // Отрежем ухо.
                    //Отрежем ухо. Скопируем хвост массива.
                    for (int j = i2; j < vertexs.Length - 1; j++)
                    {
                        vertexs[j] = vertexs[j + 1];
                    }
                    //Изменяем размер массива
                    Array.Resize(ref vertexs, vertexs.Length - 1);
                    cut = 1;
                }
                else
                {
                    i++;
                }
                if (i >= vertexs.Length)
                {
                    i = 0;
                }
                if (cut == 1000)
                {
                    //area = 0;
                    break;
                }
                cut++;
            }
            // Прибавим последний треугольник
            if (vertexs.Length == 3)
            {
                area = area + AreaTriangle(vertexs[0], vertexs[1], vertexs[2]);
            }
            return area;
        }

        //****************************************************************
        // Получить максимальное и минимальное значение
        // параметры передаются по ссылке
        //****************************************************************
        public void GetMaxMin(ref float xMin, ref float yMin, ref float xMax, ref float yMax, ref float tMaxX, ref float tMaxY)
        {
            //Пройдемся по ребрам
            for (int i = 0; i < arrVertex.Length; i++)
            {
                if (arrVertex[i].X < xMin)
                {
                    xMin = arrVertex[i].X;
                }
                if (arrVertex[i].X > xMax)
                {
                    xMax = arrVertex[i].X;
                }
                if (arrVertex[i].Y < yMin)
                {
                    yMin = arrVertex[i].Y;
                }
                if (arrVertex[i].Y > yMax)
                {
                    yMax = arrVertex[i].Y;
                }
                if ((arrVertex[i].X + arrVertex[i].Y) > (tMaxX + tMaxY))
                {
                    tMaxX = arrVertex[i].X;
                    tMaxY = arrVertex[i].Y;
                }
            }
        }

        //****************************************************************
        // Получить площадь
        //****************************************************************
        public float AreaRectangle()
        {
            float xMin = float.MaxValue;
            float yMin = float.MaxValue;
            float xMax = float.MinValue;
            float yMax = float.MinValue;
            //Пройдемся по ребрам
            for (int i = 0; i < arrVertex.Length; i++)
            {
                if (arrVertex[i].X < xMin)
                {
                    xMin = arrVertex[i].X;
                }
                if (arrVertex[i].X > xMax)
                {
                    xMax = arrVertex[i].X;
                }
                if (arrVertex[i].Y < yMin)
                {
                    yMin = arrVertex[i].Y;
                }
                if (arrVertex[i].Y > yMax)
                {
                    yMax = arrVertex[i].Y;
                }
            }
            return (xMax - xMin) * (yMax - yMin);
        }

        //****************************************************************
        // Рисует многоугольник
        //****************************************************************
        public void Draw(Graphics g, Pen pen1, Pen pen2)
        {
            // Пройдемся по списку вершин
            for (int i = 0; i < arrVertex.Length; i++)
            {
                if (i == (arrVertex.Length - 1))
                {
                    // Если фигура пострена полностью, то рисуем последнюю грань.
                    if (completed)
                    {
                        g.DrawLine(pen1, arrVertex[i], arrVertex[0]);
                    }
                }
                else
                {
                    g.DrawLine(pen1, arrVertex[i], arrVertex[i + 1]);
                }
                g.DrawEllipse(pen2, arrVertex[i].X - 3, arrVertex[i].Y - 3, 6, 6);
            }
        }

        //****************************************************************
        // Рисует многоугольник по контуру обхода
        //****************************************************************
        public void DrawBypass(Graphics g, Pen pen)
        {
            // Пройдемся по списку вершин
            if (arrVertexByPass.Length > 2)
            {
                for (int i = 0; i < arrVertexByPass.Length; i++)
                {
                    if (i == (arrVertexByPass.Length - 1))
                    {
                        // Рисуем последнюю грань.
                        g.DrawLine(pen, arrVertexByPass[i], arrVertexByPass[0]);
                    }
                    else
                    {
                        g.DrawLine(pen, arrVertexByPass[i], arrVertexByPass[i + 1]);
                    }
                }
            }
        }

        //****************************************************************
        // Проверяет отрезок со сторонами полигона на пересечение
        //****************************************************************
        public Boolean checkSection(PointF p1, PointF p2)
        {
            Boolean ret = true;
            int result;

            // Пройдемся по списку вершин
            if (arrVertex.Count() > 1)
            {
                for (int i = 0; i < arrVertex.Count() - 1; i++)
                {
                    result = intersectionSegment(arrVertex[i], arrVertex[i + 1], p1, p2).status;
                    if (i == (arrVertex.Count() - 2))
                    {
                        if (result != 2)
                        {
                            ret = false;
                            break;
                        }
                    }
                    else
                    {
                        if (i == 0)
                        {
                            if ((result != 0) && (result != 2))
                            {
                                ret = false;
                                break;
                            }
                        }
                        else
                        {
                            if (result != 0)
                            {
                                ret = false;
                                break;
                            }
                        }
                    }
                }
            }
            return ret;
        }

        //********************************************************************
        // Вычисление угла между двумя векторами через скалярное произведение
        // в градусах
        //********************************************************************
        public static float GetAngle(float x1, float y1, float x2, float y2)
        {
            float cosAngle;  //Косинус угла
            float sinPhi;    //Псевдоскалярное произведение
            float аngle;     //Угол

            if ((x1 * y1 == 0) || (x2 * y2 == 0))
            {
                return 0;
            }
            else
            {
                cosAngle = (x1 * x2 + y1 * y2) /
                           ((float)Math.Pow(x1 * x1 + y1 * y1, 2) *
                            (float)Math.Pow(x2 * x2 + y2 * y2, 2));
            }

            sinPhi = x1 * y2 - x2 * y1;
            аngle = (float)(Math.Acos(cosAngle) * 180 / Math.PI);
            if (sinPhi > 0)
            {
                аngle = -аngle;
            }

            return аngle;
        }

        //********************************************************************
        // Находит векторы привязки 
        //********************************************************************
        public static void GetVector(ref int index11, ref int index12, ref int index21, ref int index22,
                                     ref bool inFirst, PointF[] vertex, PointF[] vertex1, PointF[] vertex2)
        {
            float curLong;
            float maxLong = float.MinValue;
            int index = 0;

            inFirst = true;

            // Найдем точки в двух полигонах максимально удаленных друг от друга
            for (int i1 = 0; i1 < vertex1.Length; i1++)
            {
                for (int i2 = 0; i2 < vertex2.Length; i2++)
                {
                    curLong = (float)Math.Sqrt(Math.Pow(vertex1[i1].X - vertex2[i2].X, 2) + Math.Pow(vertex1[i1].Y - vertex2[i2].Y, 2));
                    if (curLong > maxLong)
                    {
                        maxLong = curLong;
                        index21 = i1;
                        index22 = i2;
                    }
                }
            }

            // Найдем точки в полигонe максимально удаленных друг от друга
            maxLong = float.MinValue;
            for (int i1 = 0; i1 < vertex.Length; i1++)
            {
                for (int i2 = 0; i2 < vertex.Length; i2++)
                {
                    curLong = (float)Math.Sqrt(Math.Pow(vertex[i1].X - vertex[i2].X, 2) + Math.Pow(vertex[i1].Y - vertex[i2].Y, 2));
                    if (curLong > maxLong)
                    {
                        maxLong = curLong;
                        index11 = i1;
                        index12 = i2;
                    }
                }
            }
            // Сориентируем вектора
            if (vertex[index11].X > vertex[index12].X)
            {
                index = index11;
                index11 = index12;
                index12 = index;
            }
            if (vertex[index11].X == vertex[index12].X)
            {
                if (vertex[index11].Y > vertex[index12].Y)
                {

                    index = index11;
                    index11 = index12;
                    index12 = index;
                }
            }
            if (vertex1[index21].X > vertex2[index22].X)
            {
                inFirst = !inFirst;
            }
            if (vertex1[index21].X == vertex2[index22].X)
            {
                if (vertex1[index21].Y > vertex2[index22].Y)
                {
                    inFirst = !inFirst;
                }
            }
        }
    }

    //****************************************************************
    // Класс Algorithm - класс для поиска 
    //****************************************************************
    public class Algorithm
    {
        public Poligon poligon1 = new Poligon();      // Первая фигура
        public Poligon poligon2 = new Poligon();      // Вторая фигура
        public PointF[] bestPoligon1 = new PointF[0]; // Лучшая первая
        public PointF[] bestPoligon2 = new PointF[0]; // Лучшая вторая, мы двигаем только ее
        public float deltaStep = 1F;                  // Шаг по периметру
        public float deltaAngle = 1F;                 // Шаг угла
        public int maxCountRotation = 200;            // Максимальное кол-во вращений
        public bool stop = true;                      // Параметр остановки расчёта алгоритма
        public float figArea = 0;                     // Площадь фигуры
        public float area;                            // Площадь треугольника или прямоугольника 
        public float areaCurent;                      // Текущая площадь прямоугольника
        public double percentPack;                    // Процент упаковки фигуры
        public int typeRascheta;                      //     0 - треугольник 2 фигуры  
                                                      //     1 - треугольник 2 фигуры по вершинам
                                                      //     2 - четырехугольник 1 фигура
                                                      //     3 - четырехугольник 2 фигуры
                                                      //     3 - четырехугольник 2 фигуры
        public int typeRotation;                      // Тип поворота 0 - без поворотп, 1 - поворот
        public int formSizeX;                         // Размер экрана по X
        public int formSizeY;                         // Размер экрана по Y
        public int progBar;                           // Считает процент для Progress Bar
        public float xMinRect;                        // Параметры для отрисовки
        public float xMaxRect;                        //     прямоугольника и 
        public float yMinRect;                        //     треугольника 
        public float yMaxRect;                        //     в которые
        public float xMaxTriang;                      //     упакованы      
        public float yMaxTriang;                      //     наши фигуры
        public Poligon component11 = new Poligon();   // Комплектующая фигура для отображения составной фигуры 
        public Poligon component12 = new Poligon();   // Комплектующая фигура для отображения составной фигуры 
        public Poligon component21 = new Poligon();   // Комплектующая фигура для отображения составной фигуры 
        public Poligon component22 = new Poligon();   // Комплектующая фигура для отображения составной фигуры 
        public PointF[] saveVertex;                   //Для сохранеия списка вершин
                                                      //для борьбы с систематическими ошибками 
        public void StartCalc()
        {
            PointF currentPoint;                        // Текущая точка
            float currentAngle;                         // Текущий угол
            float dX;                                   // Длина отрезка по x
            float dY;                                   // Длина отрезка по y
            float dXStep;                               // Шаг по x
            float dYStep;                               // Шаг по y
            float kf;                                   // Коэффицент для отрезка
            int colStep;                                // Количество шагов по отрезку 
            float xMin = 0;                             // Параметры прямоугольника
            float yMin = 0;                             // Параметры прямоугольника  
            float xMax = 0;                             // Параметры прямоугольника
            float yMax = 0;                             // Параметры прямоугольника
            float tMaxX = 0;                            // Параметры прямоугольника
            float tMaxY = 0;                            // Параметры прямоугольника
            int countRotation;                          // Счётчик количества вращений, для борьбы с погрешностью
            int endVertexLine;                          // Конечная вершина отрезка
            PointF mid = new PointF();                  // Центр вращения фигуры

            //Сохраним исходные фигуры
            saveVertex = new PointF[poligon1.arrVertex.Length];

            //Обнулим счетчики
            countRotation = 0;
            currentAngle = 0;
            progBar = 0;

            //Скопируем вершины
            Array.Copy(poligon1.arrVertex, saveVertex, poligon1.arrVertex.Length);

            //Расчитываем площадь фигуры
            figArea = poligon1.Area();
            if (figArea == 0)
            {
                return;
            }
            //Начальное значение площади треугольника или прямоугольника 
            area = float.MaxValue;
            //Цикл вращения для оптимальной ориентации первой фигуры
            while (currentAngle < 360)
            {
                //Обнулим параметры
                resetParam(ref xMin, ref yMin, ref xMax, ref yMax, ref tMaxX, ref tMaxY);
                //Расчитаем параметры
                poligon1.GetMaxMin(ref xMin, ref yMin, ref xMax, ref yMax, ref tMaxX, ref tMaxY);
                //Рассчитаем площадь
                if ((typeRascheta == 0) || (typeRascheta == 1))
                {
                    //Это треугольник
                    areaCurent = (float)Math.Pow(tMaxX - xMin + tMaxY - yMin, 2) / 2;
                }
                else
                {
                    //Это прямоугольник
                    areaCurent = (xMax - xMin) * (yMax - yMin);
                }
                //Центр вращения
                mid.X = (xMax + xMin) / 2;
                mid.Y = (yMax + yMin) / 2;
                //Это минимальная площадь?
                if (areaCurent < area)
                {
                    //Запомним минимальную площадь
                    area = areaCurent;
                    //Запомним минимальную фигуру
                    // Зададим размер массива
                    Array.Resize(ref bestPoligon1, poligon1.arrVertex.Length);
                    // Скопируем вершины
                    Array.Copy(poligon1.arrVertex, bestPoligon1, poligon1.arrVertex.Length);
                }
                //Повернем фигуру
                poligon1.Rotation(deltaAngle, mid);
                //Изменим угол
                currentAngle = currentAngle + deltaAngle;
            }
            //Загрузим лучший результат
            Array.Copy(bestPoligon1, poligon1.arrVertex, poligon1.arrVertex.Length);
            //Обнулим параметры
            resetParam(ref xMin, ref yMin, ref xMax, ref yMax, ref tMaxX, ref tMaxY);
            //Расчитаем параметры
            poligon1.GetMaxMin(ref xMin, ref yMin, ref xMax, ref yMax, ref tMaxX, ref tMaxY);
            //Если нужно повернуть, то повернем
            if (typeRotation == 1)
            {
                //Центр вращения
                mid.X = (xMax + xMin) / 2;
                mid.Y = (yMax + yMin) / 2;
                //Повернем на 135 градусов
                poligon1.Rotation(135, mid);
                //Запишем назад в bestPoligon1
                // Зададим размер массива
                Array.Resize(ref bestPoligon1, poligon1.arrVertex.Length);
                // Скопируем вершины
                Array.Copy(poligon1.arrVertex, bestPoligon1, poligon1.arrVertex.Length);
            }
            //Переместим в центр экрана
            poligon1.Move((formSizeX / 2) - (xMin + xMax) / 2, (formSizeY / 2) - (yMin + yMax) / 2);
            //Обнулим параметры
            resetParam(ref xMin, ref yMin, ref xMax, ref yMax, ref tMaxX, ref tMaxY);
            //Расчитаем параметры
            poligon1.GetMaxMin(ref xMin, ref yMin, ref xMax, ref yMax, ref tMaxX, ref tMaxY);
            //Если оцениваем одну фигуру в четырехугольнике
            if (typeRascheta == 2)
            {
                //Рассчитаем процент упаковки
                percentPack = (double)Math.Round((double)(figArea * 100 / area), 1, MidpointRounding.AwayFromZero);
                //Запомним треугольник и прямоугольник
                xMinRect = xMin;
                xMaxRect = xMax;
                yMinRect = yMin;
                yMaxRect = yMax;
                xMaxTriang = tMaxX;
                yMaxTriang = tMaxY;
                return;
            }

            //Покажем что запустили
            stop = false;
            //Обнулим решение
            area = float.MaxValue;
            bestPoligon2 = new PointF[0];
            bestPoligon1 = new PointF[0];
            //Создадим контур обхода 1-ой фигуры
            poligon1.CreateVertexByPass();
            // Цикл по вершинам второй фигуры
            for (int i = 0; i < poligon2.arrVertex.Length; i++)
            {
                // Цикл по вершинам контура обхода первой фигуры
                for (int j = 0; j < poligon1.arrVertexByPass.Length; j++)
                {
                    //Восстановим массив вершин 2-ой фигуры
                    Array.Copy(saveVertex, poligon2.arrVertex, saveVertex.Length);
                    //Текущая точка на контуре обхода первой фигуры
                    currentPoint = poligon1.arrVertexByPass[j];
                    //Переместим в эту точку вторую фигуру
                    poligon2.Move(currentPoint.X - poligon2.arrVertex[i].X, currentPoint.Y - poligon2.arrVertex[i].Y);
                    //Вычислим приращение шага по x и по y 
                    if (j == poligon1.arrVertexByPass.Length - 1)
                    {
                        endVertexLine = 0;
                    }
                    else
                    {
                        endVertexLine = j + 1;
                    }
                    dX = poligon1.arrVertexByPass[endVertexLine].X - poligon1.arrVertexByPass[j].X;
                    dY = poligon1.arrVertexByPass[endVertexLine].Y - poligon1.arrVertexByPass[j].Y;
                    if (Math.Abs(dX) < Poligon.delta)
                    {
                        dXStep = 0;
                        dYStep = Math.Sign(dY) * deltaStep;
                    }
                    else
                    {
                        kf = dY / dX;
                        dXStep = Math.Sign(dX) * deltaStep / (float)Math.Sqrt(1 + kf * kf);
                        dYStep = kf * dXStep;
                    }
                    //Вычислим количество шагов
                    colStep = (int)Math.Floor(Math.Sqrt(dX * dX + dY * dY) / deltaStep);
                    //Если обходим по треугольнику только по вершинам, то обнулим colStep
                    if (typeRascheta == 1)
                    {
                        colStep = 0;
                    }
                    //Пройдемся по грани
                    for (int k = 0; k <= colStep; k++)
                    {
                        currentAngle = 0;
                        //Здесь мы вращаем
                        while (currentAngle < 360)
                        {
                            //Если фигуры не пересекаются
                            if (Poligon.CheckPoligon(ref poligon1, ref poligon2))
                            {
                                //Обнулим параметры
                                resetParam(ref xMin, ref yMin, ref xMax, ref yMax, ref tMaxX, ref tMaxY);
                                //Расчитаем параметры
                                poligon1.GetMaxMin(ref xMin, ref yMin, ref xMax, ref yMax, ref tMaxX, ref tMaxY);
                                poligon2.GetMaxMin(ref xMin, ref yMin, ref xMax, ref yMax, ref tMaxX, ref tMaxY);
                                //Рассчитаем площадь
                                if ((typeRascheta == 0) || (typeRascheta == 1))
                                {
                                    //Это треугольник
                                    areaCurent = (float)Math.Pow(tMaxX - xMin + tMaxY - yMin, 2) / 2;
                                }
                                else
                                {
                                    //Это прямоугольник
                                    areaCurent = (xMax - xMin) * (yMax - yMin);
                                }
                                //Это минимальная площадь?
                                if (areaCurent < area)
                                {
                                    //Запомним минимальную площадь
                                    area = areaCurent;
                                    //Рассчитаем процент упаковки
                                    percentPack = (double)Math.Round((double)(figArea * 2 * 100 / area), 1, MidpointRounding.AwayFromZero);
                                    //Запомним треугольник и прямоугольник
                                    xMinRect = xMin;
                                    xMaxRect = xMax;
                                    yMinRect = yMin;
                                    yMaxRect = yMax;
                                    xMaxTriang = tMaxX;
                                    yMaxTriang = tMaxY;
                                    //Запомним минимальную фигуру
                                    // Зададим размер
                                    Array.Resize(ref bestPoligon2, poligon2.arrVertex.Length);
                                    // Скопируем вершины
                                    Array.Copy(poligon2.arrVertex, bestPoligon2, poligon2.arrVertex.Length);
                                }
                            }
                            //Повернем фигуру
                            poligon2.Rotation(deltaAngle, currentPoint);
                            //Увеличим счетчик поворотов
                            countRotation++;
                            //Увеличим текущий угол
                            currentAngle = currentAngle + deltaAngle;
                            //Если нас хотят остановить просто выйдем
                            if (stop)
                            {
                                return;
                            }
                        }
                        //Если нужно востановим вершины (защита от систематической ошибки)
                        if (countRotation > maxCountRotation)
                        {
                            //Обнулим счетчик поворотов
                            countRotation = 0;
                            //Восстановим массив вершин 2-ой фигуры
                            Array.Copy(saveVertex, poligon2.arrVertex, saveVertex.Length);
                            //Переместим в эту точку вторую фигуру
                            poligon2.Move(currentPoint.X - poligon2.arrVertex[i].X + dXStep, currentPoint.Y - poligon2.arrVertex[i].Y + dYStep);
                        }
                        //Выберим следующую точку
                        currentPoint.X = currentPoint.X + dXStep;
                        currentPoint.Y = currentPoint.Y + dYStep;
                    }
                    //Изменим прогрессбар
                    progBar = (int)((i * poligon1.arrVertexByPass.Length + (j + 1)) * 100 /
                                    (poligon2.arrVertex.Length * poligon1.arrVertexByPass.Length));
                    if (progBar > 100)
                    {
                        progBar = 100;
                    }
                }
            }
            //Изменим прогрессбар
            progBar = 100;
            //Восстановим массив вершин 2-ой фигуры
            Array.Copy(saveVertex, poligon2.arrVertex, saveVertex.Length);
            //Покажем что расчет заверше
            stop = true;
        }

        //***************************************************************
        // Сброс параметров
        //***************************************************************
        void resetParam(ref float mxMin, ref float myMin, ref float mxMax,
                        ref float myMax, ref float mtMaxX, ref float mtMaxY)
        {
            mxMin = float.MaxValue;
            myMin = float.MaxValue;
            mxMax = float.MinValue;
            myMax = float.MinValue;
            mtMaxX = float.MinValue;
            mtMaxY = float.MinValue;
        }

        //****************************************************************
        // Рисует многоугольники, текущее положение, при расчете может
        // рисовать не корректно
        //****************************************************************
        public void Draw(Graphics g, Pen pen)
        {
            //Рисуем 1-ую фигуру
            poligon1.Draw(g, pen);
            //Рисуем 2-ую фигуру
            poligon2.Draw(g, pen);
            //Это не обязательно рисуем путь обхода
            poligon1.DrawBypass(g, pen);
        }

        //****************************************************************
        // Рисует многоугольники 1 и лучший вариант 2-ой
        //****************************************************************
        public void DrawBest(Graphics g, Pen pen1, Pen pen2)
        {

            // Рисуем фигуру в которую висываем фигуры
            if ((typeRascheta == 0) || (typeRascheta == 1))
            {
                //Это треугольник
                g.DrawLine(pen1, xMinRect - 2, yMinRect - 2, xMaxTriang + yMaxTriang - yMinRect + 2, yMinRect - 2);
                g.DrawLine(pen1, xMinRect - 2, yMinRect - 2, xMinRect - 2, yMaxTriang + xMaxTriang - xMinRect + 2);
                g.DrawLine(pen1, xMaxTriang + yMaxTriang - yMinRect + 2, yMinRect - 2, xMinRect - 2, yMaxTriang + xMaxTriang - xMinRect + 2);
            }
            else
            {
                //Это прямоугольник
                g.DrawRectangle(pen1, xMinRect - 2, yMinRect - 2, xMaxRect - xMinRect + 4, yMaxRect - yMinRect + 4);
            }
            //Рисуем 1-ую фигуру
            poligon1.Draw(g, pen2);
            //Рисуем лучший вариант 2-ой фигуры
            if (bestPoligon2.Length > 2)
            {
                for (int i = 0; i < bestPoligon2.Length; i++)
                {
                    if (i == (bestPoligon2.Length - 1))
                    {
                        g.DrawLine(pen2, bestPoligon2[i], bestPoligon2[0]);
                    }
                    else
                    {
                        g.DrawLine(pen2, bestPoligon2[i], bestPoligon2[i + 1]);
                    }
                }
            }
        }

        //*********************************************************************
        // Поворачивает исходные фигуры в составной фигуре после вычисления
        //*********************************************************************
        public void RotationComposit()
        {
            float dX;              //Смещение по X
            float dY;              //Смещение по Y
            float angle;           //Угол поаорота
            int index11 = 0;
            int index12 = 0;
            int index21 = 0;
            int index22 = 0;
            bool inFirst = true;
            PointF rPoint;         //Точка вращения

            //Проверим размеры 
            if ((saveVertex.Length < 2) || (poligon1.arrVertex.Length < 2) || (bestPoligon2.Length < 2))
            {
                return;
            }
            //Посмотрим первую фигуру
            //Получим вектора привязки
            Poligon.GetVector(ref index11, ref index12, ref index21, ref index22, ref inFirst, saveVertex, component11.arrVertex, component12.arrVertex);
            //Переместим и повернеи
            dX = poligon1.arrVertex[index11].X - saveVertex[index11].X;
            dY = poligon1.arrVertex[index11].Y - saveVertex[index11].Y;
            angle = Poligon.GetAngle(poligon1.arrVertex[index12].X - poligon1.arrVertex[index11].X,
                                     poligon1.arrVertex[index12].Y - poligon1.arrVertex[index11].Y,
                                     saveVertex[index12].X - saveVertex[index11].X,
                                     saveVertex[index12].Y - saveVertex[index11].Y);
            component11.Move(dX, dY);
            component12.Move(dX, dY);
            if (inFirst)
            {
                rPoint = component11.arrVertex[index21];
            }
            else
            {
                rPoint = component12.arrVertex[index22];
            }
            component11.Rotation(angle, rPoint);
            component12.Rotation(angle, rPoint);

            //Посмотрим вторую фигуру
            //Получим вектора привязки
            index11 = 0;
            index12 = 0;
            index21 = 0;
            index22 = 0;
            inFirst = true;
            Poligon.GetVector(ref index11, ref index12, ref index21, ref index22, ref inFirst, saveVertex, component21.arrVertex, component22.arrVertex);
            //Переместим и повернеи
            dX = bestPoligon2[index11].X - saveVertex[index11].X;
            dY = bestPoligon2[index11].Y - saveVertex[index11].Y;
            angle = Poligon.GetAngle(bestPoligon2[index12].X - bestPoligon2[index11].X,
                                     bestPoligon2[index12].Y - bestPoligon2[index11].Y,
                                     saveVertex[index12].X - saveVertex[index11].X,
                                     saveVertex[index12].Y - saveVertex[index11].Y);
            component21.Move(dX, dY);
            component22.Move(dX, dY);
            if (inFirst)
            {
                rPoint = component21.arrVertex[index21];
            }
            else
            {
                rPoint = component22.arrVertex[index22];
            }
            component21.Rotation(angle, rPoint);
            component22.Rotation(angle, rPoint);

        }

        //*********************************************************************
        // Рисует многоугольники 1 и лучший вариант 2-ой для составной фигуры
        //*********************************************************************
        public void DrawBestComposit(Graphics g, Pen pen1, Pen pen2)
        {
            // Рисуем фигуру в которую вписываем фигуры
            if ((typeRascheta == 0) || (typeRascheta == 1))
            {
                //Это треугольник
                g.DrawLine(pen1, xMinRect - 2, yMinRect - 2, xMaxTriang + yMaxTriang - yMinRect + 2, yMinRect - 2);
                g.DrawLine(pen1, xMinRect - 2, yMinRect - 2, xMinRect - 2, yMaxTriang + xMaxTriang - xMinRect + 2);
                g.DrawLine(pen1, xMaxTriang + yMaxTriang - yMinRect + 2, yMinRect - 2, xMinRect - 2, yMaxTriang + xMaxTriang - xMinRect + 2);
            }
            else
            {
                //Это прямоугольник
                g.DrawRectangle(pen1, xMinRect - 2, yMinRect - 2, xMaxRect - xMinRect + 4, yMaxRect - yMinRect + 4);
            }

            component11.Draw(g, pen2);
            component12.Draw(g, pen2);
            component21.Draw(g, pen2);
            component22.Draw(g, pen2);
        }


        //*************************************************************************
        // Рисует массив 
        //*************************************************************************
        public void DrawArray(PointF[] pArr, Graphics g, Pen pen, float kF, float offsetX, float offsetY, int orient, StreamWriter sw)
        {
            Matrix fMatrix = new Matrix();
            PointF[] arr = new PointF[0];

            if(pArr.Length==0)
            {
                return;
            }

            //Запишем в файл
            try
            {
                sw.WriteLine();
            }
            catch
            {
            }

            // Это первая фигура
            // Зададим размер массива
            Array.Resize(ref arr, pArr.Length);
            // Скопируем вершины
            Array.Copy(pArr, arr, pArr.Length);
            // Переместим в начальную точку
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i].X = arr[i].X - xMinRect;
                arr[i].Y = arr[i].Y - yMinRect;
            }

            if (orient == 1)
            {
                //Сбросим матрицу преобразования
                fMatrix.Reset();
                //Повернем
                fMatrix.Rotate(90F);
                //Применим преобразование
                fMatrix.TransformPoints(arr);
                // Переместим
                for (int i = 0; i < arr.Length; i++)
                {
                    arr[i].X = arr[i].X + yMaxRect - yMinRect;
                }
            }

            //Сбросим матрицу преобразования
            fMatrix.Reset();
            //Изменим маштаб
            fMatrix.Scale(kF, kF);
            //Применим преобразование
            fMatrix.TransformPoints(arr);

            // Переместим в нужную точку точку
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i].X = arr[i].X + offsetX;
                arr[i].Y = arr[i].Y + offsetY;
            }

            //Нарисуем
            if (arr.Length > 2)
            {
                for (int i = 0; i < arr.Length; i++)
                {
                    if (i == (arr.Length - 1))
                    {
                        g.DrawLine(pen, arr[i], arr[0]);
                    }
                    else
                    {
                        g.DrawLine(pen, arr[i], arr[i + 1]);
                    }
                    //Запишем в файл
                    try
                    {
                        sw.Write(arr[i].X.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) +","+
                                 arr[i].Y.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + ";");
                    }
                    catch 
                    {
                    }
                }
            }
        }


        //*********************************************************************
        // Рисует многоугольники 1 и лучший вариант 2-ой для карты раскроя
        //*********************************************************************
        public void DrawFigurInCard(Graphics g, Pen pen, float kF, float offsetX, float offsetY, int orient, StreamWriter sw)
        {
            DrawArray(poligon1.arrVertex, g, pen, kF, offsetX, offsetY, orient, sw);
            DrawArray(bestPoligon2, g, pen, kF, offsetX, offsetY, orient, sw);
        }

        //*********************************************************************************************
        // Рисует многоугольники 1 и лучший вариант 2-ой для карты раскроя для составной фигуры
        //*********************************************************************************************
        public void DrawFigurInCardComposit(Graphics g, Pen pen, float kF, float offsetX, float offsetY, int orient, StreamWriter sw)
        {
            DrawArray(component11.arrVertex, g, pen, kF, offsetX, offsetY, orient, sw);
            DrawArray(component12.arrVertex, g, pen, kF, offsetX, offsetY, orient, sw);
            DrawArray(component21.arrVertex, g, pen, kF, offsetX, offsetY, orient, sw);
            DrawArray(component22.arrVertex, g, pen, kF, offsetX, offsetY, orient, sw);
        }








    }
}
