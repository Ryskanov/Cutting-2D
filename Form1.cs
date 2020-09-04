using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections;
using System.Threading;
using System.Globalization;
using System.IO;

namespace RyskanovDiplom
{
    //**************************************************************************
    // Структура для передачи параметров при запуске прцедуры расчета в потоке
    //**************************************************************************
    public struct ParamForGo
    {
        public Project project;
        public Figur figur;
        public ArrayList arrListVertex;
        public ParamForGo(Project m_project, Figur m_figur, ArrayList m_arrListVertex)
        {
            project = m_project;
            figur = m_figur;
            arrListVertex = m_arrListVertex;
        }
        public override string ToString() => $"{project.name}";
    }

    //**********************************************************************
    // Основная форма программы
    //**********************************************************************
    public partial class Form1 : Form
    {
        //Подключим функцию из внешней библиотеки для блокировки перерисовки при изменении размеров окна LockWindowUpdate
        [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern Int32 LockWindowUpdate(IntPtr Handle);

        DB dateBase;                                //База данных
        Figur curentFigur = new Figur(0, "");       //Текущая фигура
        int statusFigur = 0;                        //Статус фигуры. 0 - не редактируется, 1 - редактируется
        int curentFigurIndex = -1;                  //Индекс текущей фигуры
        Poligon poligon;                            //Многоугольник
        Pen penPoligon = new Pen(Color.Blue, 3);    //Линия Многоугольника
        Pen penPoint = new Pen(Color.Red, 3);       //Линия Точки
        int statusDraw = 0;                         //Режим рисования

        Graphics gGrid;                             //Грвфика для сетки
        int ScreenWidth;                            //Ширина экрана   
        int ScreenHeight;                           //Высота экрана
        Pen penLine = new Pen(Color.LightGray, 1);  //Линия сетки
        Pen penLine2 = new Pen(Color.Gray, 1);      //Линия сетки темные
        Pen penLine3 = new Pen(Color.Blue, 3);      //Линия ординат

        Font textFont = new Font("Arial", 8);               //Шрифт для печати на графике фигуры
        SolidBrush textBrush = new SolidBrush(Color.Blue);  //Цвет для печати на графике фигуры

        Graphics gFigur;                            //Грвфика для фигуры
        Pen penCross = new Pen(Color.Green, 3);     //Крестик
        int OldCursorX = 0;                         //Старая координата X  
        int OldCursorY = 0;                         //Старая координата Y

        Boolean newProect = false;                  //Признак новый проект
        Boolean editProect = false;                 //Признак проект редактируется

        int statusProject;                          //Статус проекта 0-не запущен, 1-запущен
        Graphics gCalc;                             //Грвфика для расчета
        Pen penРerimetr = new Pen(Color.Green, 5);  //Линия периметра
        Pen penPage = new Pen(Color.Cyan, 5);       //Линия страницы

        Algorithm algorithm1;                       //Объект для рассчета
        Algorithm algorithm2;                       //Объект для рассчета
        Algorithm algorithm3;                       //Объект для рассчета
        Algorithm algorithm4;                       //Объект для рассчета
        Algorithm algorithm5;                       //Объект для рассчета

        Poligon unionPoligon1;                      //Объединенная фигура 
        Poligon unionPoligon2;                      //Объединенная фигура 

        string title1 = "Раскрой листового материала."; //Заголовок программы
        string title2 = " (Режим редактирования)";      //Заголовок программы

        //Конструктор формы
        public Form1()
        {
            InitializeComponent();

            //Получим размер экрана
            ScreenWidth = Screen.PrimaryScreen.Bounds.Width;
            ScreenHeight = Screen.PrimaryScreen.Bounds.Height;

            // Создать графику для сетки
            // Создаём Bitmap с размером области вывода
            Bitmap bitmapGrid = new Bitmap(ScreenWidth, ScreenHeight);
            //Указываем рисовать графику из Bitmap
            gGrid = Graphics.FromImage(bitmapGrid);
            //Устанавливаем задний фон из Bitmap  
            pictureBox1.BackgroundImage = bitmapGrid;

            // Создать графику для фигуры
            // Создаём Bitmap с размером области вывода
            Bitmap bitmapFigur = new Bitmap(ScreenWidth, ScreenHeight);
            //Указываем рисовать графику из Bitmap
            gFigur = Graphics.FromImage(bitmapFigur);
            //Устанавливаем задний фон из Bitmap  
            pictureBox2.BackgroundImage = bitmapFigur;
            //Это нужно для прозрачного фона
            pictureBox2.Parent = pictureBox1;

            //Многоугольник
            poligon = new Poligon();

            //Настроим колонки таблицы
            var column0 = new DataGridViewColumn();
            column0.Visible = false;
            column0.Frozen = true;
            column0.Name = "cod";
            column0.CellTemplate = new DataGridViewTextBoxCell();

            var column1 = new DataGridViewColumn();
            column1.HeaderText = "Название";
            column1.Width = 180;
            column1.ReadOnly = true;
            column1.Name = "name";
            column1.Frozen = true;
            column1.CellTemplate = new DataGridViewTextBoxCell();

            var column2 = new DataGridViewColumn();
            column2.HeaderText = "Кло-во";
            column2.Name = "col";
            column2.Frozen = true;
            column2.CellTemplate = new DataGridViewTextBoxCell();

            dataGridView1.Columns.Add(column0);
            dataGridView1.Columns.Add(column1);
            dataGridView1.Columns.Add(column2);

            // Создать графику для расчета
            // Создаём Bitmap с размером области вывода
            Bitmap bitmapCalc = new Bitmap(ScreenWidth, ScreenHeight);
            //Указываем рисовать графику из Bitmap
            gCalc = Graphics.FromImage(bitmapCalc);
            //Устанавливаем задний фон из Bitmap  
            pictureBox3.BackgroundImage = bitmapCalc;

            //Загрузим данные из базы данных
            LoadData();

            //Загрузим справочную информацию
            try
            {
                richTextBox1.LoadFile("ReadMy.rtf");
            }
            catch
            {
            }
        }

        //****************************************
        // Загрузка данных из базы данных
        //****************************************
        void LoadData()
        {
            dateBase = new DB();
            dateBase.CreateDB();
            dateBase.OpenDB();
            LoadFigur();
            DrawGrid();
        }

        //****************************************
        // Загрузка списка фигур
        //****************************************
        void LoadFigur()
        {
            //Очистить лист
            listBox1.Items.Clear();
            //Прочитаем из базы список фигур
            ArrayList arrList = dateBase.ReadListFigur();
            //Заполним список
            listBox1.BeginUpdate();
            foreach (Figur figur in arrList)
            {
                listBox1.Items.Add(figur);
            }
            listBox1.EndUpdate();
            if (listBox1.Items.Count > 0)
            {
                listBox1.SelectedIndex = 0;
                SelectFigur();
            }
        }

        //****************************************
        // Загрузка вершин фигуры
        //****************************************
        void LoadVertex()
        {
            //Прочитаем из базы список вершин 
            ArrayList arrListVertex = dateBase.ReadListVertexFigur(curentFigur);
            //Создадим многоугольник и заполним вершины
            poligon = new Poligon();
            poligon.completed = true;
            foreach (Vertex vertex in arrListVertex)
            {
                poligon.AddVertex(new PointF(vertex.x, vertex.y));
            }
        }

        //****************************************
        // Действия при выборе фигуры
        //****************************************
        void SelectFigur()
        {
            if (listBox1.SelectedIndex < 0)
            {
                if (listBox1.Items.Count > 0)
                {
                    listBox1.SelectedIndex = 0;
                }
            }
            if (listBox1.SelectedIndex >= 0)
            {
                //Текущий индекс в списке фигур
                curentFigurIndex = listBox1.SelectedIndex;
                //Текущия фигура 
                curentFigur = (Figur)listBox1.Items[curentFigurIndex];
            }
            else
            {
                //Текущий индекс в списке фигур
                curentFigurIndex = -1;
                //Текущия фигура 
                curentFigur = new Figur(0, "");
            }
            //Параметры фигуры
            textBox1.Text = curentFigur.name;
            statusFigur = 0;
            //Прочитаем вершины и заполним многоугольник
            LoadVertex();
            //Нарисуем фигуру
            gFigur.Clear(Color.Transparent);
            poligon.Draw(gFigur, penPoligon, penPoint);
            pictureBox2.Invalidate();
        }

        //****************************************
        // Сохранить фигуру
        //****************************************
        void SaveFigur()
        {
            if (curentFigur.cod > 0)
            {
                dateBase.DeleteVertexFigur(curentFigur);
                dateBase.WriteFigur(curentFigur);
            }
            else
            {
                curentFigur.cod = dateBase.AddFigur(curentFigur);
            }
            if (curentFigur.cod > 0)
            {
                int i = 1;
                ArrayList arrList = new ArrayList();
                foreach (PointF point in poligon.arrVertex)
                {
                    arrList.Add(new Vertex(curentFigur.cod, i, point.X, point.Y));
                    i++;
                }
                dateBase.AddVertexFigur(arrList);
            }

            long cod = curentFigur.cod;
            LoadFigur();
            for (int i = 0; i < listBox1.Items.Count; i++)
            {
                if (cod == ((Figur)listBox1.Items[i]).cod)
                {
                    listBox1.SelectedIndex = i;
                    SelectFigur();
                    break;
                }
            }
        }

        //****************************************************************
        // Удалить фигуру
        //****************************************************************
        void DeleteFigur()
        {
            if (dateBase.DeleteFigur(curentFigur) == 0)
            {
                listBox1.Items.RemoveAt(listBox1.SelectedIndex);
            }
        }

        //****************************************************************
        // Рисует сетку
        //****************************************************************
        public void DrawGrid()
        {
            for (int i = 0; i <= ScreenWidth; i += 20)
            {
                if (i % 100 == 0)
                {
                    // Отрисовка вертикальных линий синий
                    gGrid.DrawLine(penLine2, i, 0, i, ScreenHeight);
                }
                else
                {
                    // Отрисовка  вертикальных линий зеленый
                    gGrid.DrawLine(penLine, i, 0, i, ScreenHeight);
                }
            }
            for (int i = 0; i <= ScreenHeight; i += 20)
            {
                if (i % 100 == 0)
                {
                    // Отрисовка горризонтальных линий синий
                    gGrid.DrawLine(penLine2, 0, i, ScreenWidth, i);
                }
                else
                {
                    // Отрисовка горризонтальных линий зеленый
                    gGrid.DrawLine(penLine, 0, i, ScreenWidth, i);
                }
            }
            // Отрисовка ординат
            gGrid.DrawLine(penLine3, 0, 0, ScreenWidth, 0);
            gGrid.DrawLine(penLine3, 0, 0, 0, ScreenHeight);
            // Напечатаем начало координат
            gGrid.DrawString("0,0", textFont, textBrush, new PointF(5, 5));
        }

        //****************************************************************
        // Рисует курсор
        //****************************************************************
        public void CalcCursor(ref int x, ref int y)
        {
            int dX = x % 20;
            int dY = y % 20;

            x -= dX;
            y -= dY;

            if (dX > 10)
            {
                x += 20;
            }
            if (dY > 10)
            {
                y += 20;
            }
        }

        //************************************************************************************************************
        //    ОБРАБОТЧИКИ СОБЫТИй
        //************************************************************************************************************

        //****************************************
        // Событие при изменении выбранной фигуры
        //****************************************
        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            SelectFigur();
        }

        //****************************************
        // Событие начало изменения размера формы
        //****************************************
        private void Form1_ResizeBegin(object sender, EventArgs e)
        {
            //Блокировка 
            LockWindowUpdate(Handle);
        }

        //****************************************
        // Событие конец изменения размера формы
        //****************************************
        private void Form1_ResizeEnd(object sender, EventArgs e)
        {
            //Разблокировка 
            LockWindowUpdate(IntPtr.Zero);
        }

        //****************************************
        // Перемещение мышки по фигуре
        //****************************************
        private void pictureBox2_MouseMove(object sender, MouseEventArgs e)
        {
            //Если редактирование не влючено выходим
            if (statusDraw == 0)
            {
                return;
            }

            //Сохраним координаты
            int X = e.X;
            int Y = e.Y;

            //Получим координаты курсора
            CalcCursor(ref X, ref Y);
            //Нужно ли перерисовать курсор
            if ((X != OldCursorX) || (Y != OldCursorY))
            {
                //Запомним положение курсора
                OldCursorX = X;
                OldCursorY = Y;
                //Очистим 
                gFigur.Clear(Color.Transparent);
                //Нарисуем многоугольник
                poligon.Draw(gFigur, penPoligon, penPoint);
                //Нарисуем последнюю линию
                if (poligon.arrVertex.Length > 0)
                {
                    gFigur.DrawLine(penPoligon, poligon.arrVertex[poligon.arrVertex.Length - 1].X, poligon.arrVertex[poligon.arrVertex.Length - 1].Y, X, Y);
                }
                //Нарисуем крестик
                gFigur.DrawLine(penCross, X - 5, Y, X + 5, Y);
                gFigur.DrawLine(penCross, X, Y - 5, X, Y + 5);
                //Обновим изображение
                pictureBox2.Invalidate();
                label2.Text = "X: " + X.ToString();
                label2.Invalidate();
                label3.Text = "Y: " + Y.ToString();
            }

        }

        //****************************************
        // Нажатие мышки на фигуре
        //****************************************
        private void pictureBox2_MouseDown(object sender, MouseEventArgs e)
        {
            int X = e.X;
            int Y = e.Y;
            bool stop = false;

            if (statusDraw == 1)
            {
                //Получим координаты курсора
                CalcCursor(ref X, ref Y);
                //Проверим на корректность
                if (poligon.checkVertex(new PointF(X, Y)))
                {
                    //Если введенная вершина это первая вершина то завершить редактирование
                    if (poligon.arrVertex.Length > 1)
                    {
                        float S = (float)Math.Sqrt(Math.Pow(X - poligon.arrVertex[0].X, 2) +
                                                   Math.Pow(Y - poligon.arrVertex[0].Y, 2));
                        if (S < Poligon.delta)
                        {
                            poligon.completed = true;
                            statusDraw = 0;
                            stop = true;
                            //Очистим 
                            gFigur.Clear(Color.Transparent);
                            //Нарисуем многоугольник
                            poligon.Draw(gFigur, penPoligon, penPoint);
                            //Обновим изображение
                            pictureBox2.Invalidate();
                        }
                    }
                    //Добавим вершину
                    if (!stop)
                    {
                        poligon.AddVertex(new PointF(X, Y));
                    }
                }
            }

        }

        //****************************************
        // Редактировать
        //****************************************
        private void button4_Click(object sender, EventArgs e)
        {
            if ((curentFigur.cod != 0) && (statusFigur == 0))
            {
                statusFigur = 1;
                statusDraw = 1;
                textBox1.ReadOnly = false;
                listBox1.Enabled = false;
                poligon.completed = false;
                poligon.DeleteAllVertex();
                gFigur.Clear(Color.Transparent);
                pictureBox2.Invalidate();
                this.Text = title1 + title2;
            }
        }

        //****************************************
        // Отменить редактирование
        //****************************************
        private void button5_Click(object sender, EventArgs e)
        {
            SelectFigur();
            textBox1.ReadOnly = true;
            listBox1.Enabled = true;
            statusFigur = 0;
            statusDraw = 0;
            this.Text = title1;
        }


        //****************************************
        // Сохранить фигуру
        //****************************************
        private void button3_Click(object sender, EventArgs e)
        {
            if (statusFigur == 1)
            {
                if (statusDraw == 1)
                {
                    MessageBox.Show("Необходимо завершить построение фигуры.", "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                DialogResult result = MessageBox.Show("Сохранить данные?", "Внимание!", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (result == DialogResult.Yes)
                {
                    SaveFigur();
                }
                else
                {
                    SelectFigur();
                }
                textBox1.ReadOnly = true;
                listBox1.Enabled = true;
                statusFigur = 0;
                statusDraw = 0;
                this.Text = title1;
            }
        }

        //****************************************
        // Новая фигура
        //****************************************
        private void button1_Click(object sender, EventArgs e)
        {
            curentFigur = new Figur(0, "Новая фигура");
            if (statusFigur == 0)
            {
                statusFigur = 1;
                statusDraw = 1;
                textBox1.ReadOnly = false;
                textBox1.Text = curentFigur.name;
                listBox1.Enabled = false;
                poligon.completed = false;
                poligon.DeleteAllVertex();
                gFigur.Clear(Color.Transparent);
                pictureBox2.Invalidate();
                this.Text = title1 + title2;
            }
        }

        //****************************************
        // Удалить фигуру
        //****************************************
        private void button2_Click(object sender, EventArgs e)
        {
            if ((curentFigur.cod != 0) && (statusFigur == 0) && (curentFigurIndex > -1))
            {
                DialogResult result = MessageBox.Show("Удалить фигуру?", "Внимание!", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (result == DialogResult.Yes)
                {
                    DeleteFigur();
                }
            }
        }

        //****************************************
        // Изменение наименования
        //****************************************
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            curentFigur.name = textBox1.Text;
        }

        //****************************************
        // Блокируем выбор вкладки
        //****************************************
        private void tabControl1_Selecting(object sender, TabControlCancelEventArgs e)
        {
            //Редактируем фигуру переключение не возможно
            if (statusFigur == 1)
            {
                if (tabControl1.SelectedIndex > 0)
                {
                    tabControl1.SelectTab(0);
                    return;
                }
            }
            else
            {
                //Редактируем проект переключение не возможно
                if (editProect)
                {
                    if ((tabControl1.SelectedIndex == 0) || (tabControl1.SelectedIndex == 2))
                    {
                        tabControl1.SelectTab(1);
                        return;
                    }
                }
                else
                {
                    //Запущен расчет проект переключение не возможно
                    if ((statusProject > 0) && (statusProject < 9))
                    {
                        if ((tabControl1.SelectedIndex == 0) || (tabControl1.SelectedIndex == 1))
                        {
                            tabControl1.SelectTab(2);
                            return;
                        }
                    }
                }
            }

            //Открываем страницу проектов
            if (tabControl1.SelectedIndex == 1)
            {
                //Загрузим фигуры
                LoadFigur2();
                //Загрузим проекты
                LoadProjects();
            }
            //Открываем страницу расчета проектов
            if (tabControl1.SelectedIndex == 2)
            {
                //Загрузим проекты
                LoadProjects2();
            }
        }

        //************************************************************************************************************
        //    Проект
        //************************************************************************************************************

        //********************************************
        // Установка доступности элементов управления
        // type = 0 - просмотр
        // type = 1 - редактирование
        //********************************************
        void AccessControlProject(int type)
        {
            if (type == 0)
            {
                //Разрешим смену проекта
                listBox2.Enabled = true;
                //Запретим доступ к полям и кнопкам
                dataGridView1.ReadOnly = true;
                textBox2.ReadOnly = true;
                numericUpDown1.ReadOnly = true;
                numericUpDown2.ReadOnly = true;
                numericUpDown3.ReadOnly = true;
                numericUpDown4.ReadOnly = true;
                numericUpDown1.Increment = 0;
                numericUpDown2.Increment = 0;
                numericUpDown3.Increment = 0;
                numericUpDown4.Increment = 0;
                button11.Enabled = false;
                button12.Enabled = false;
                this.Text = title1;
            }
            else
            {
                //Запретим смену проекта
                listBox2.Enabled = false;
                //Разрешим доступ к полям и кнопкам
                dataGridView1.ReadOnly = false;
                textBox2.ReadOnly = false;
                numericUpDown1.ReadOnly = false;
                numericUpDown2.ReadOnly = false;
                numericUpDown3.ReadOnly = false;
                numericUpDown4.ReadOnly = false;
                numericUpDown1.Increment = 1;
                numericUpDown2.Increment = 1;
                numericUpDown3.Increment = 1;
                numericUpDown4.Increment = 1;
                button11.Enabled = true;
                button12.Enabled = true;
                this.Text = title1 + title2;
            }
        }

        //********************************************
        // Значения по умолчанию
        //********************************************
        void SetFieldProject()
        {
            dataGridView1.Rows.Clear();
            textBox2.Text = "";
            numericUpDown1.Value = 210;
            numericUpDown2.Value = 297;
            numericUpDown3.Value = 5;
            numericUpDown4.Value = 2;
        }



        //****************************************
        // Загрузить фигуры
        //****************************************
        void LoadFigur2()
        {
            //Очистить лист
            listBox3.Items.Clear();
            //Прочитаем из базы список фигур
            ArrayList arrList = dateBase.ReadListFigur();
            //Заполним список
            listBox3.BeginUpdate();
            foreach (Figur figur in arrList)
            {
                listBox3.Items.Add(figur);
            }
            listBox3.EndUpdate();
            if (listBox3.Items.Count > 0)
            {
                listBox3.SelectedIndex = 0;
            }
        }


        //****************************************
        // Загрузить проекты
        //****************************************
        void LoadProjects()
        {
            //Очистить лист
            listBox2.Items.Clear();
            //Прочитаем из базы список фигур
            ArrayList arrList = dateBase.ReadListProject();
            //Заполним список
            listBox2.BeginUpdate();
            foreach (Project project in arrList)
            {
                listBox2.Items.Add(project);
            }
            listBox2.EndUpdate();
            if (listBox2.Items.Count > 0)
            {
                listBox2.SelectedIndex = 0;
                SelectFigurPr();
            }
            //Установим доступность элементов управления
            AccessControlProject(0);
        }

        //****************************************
        // Найти имя фигуры
        //****************************************
        string NameFigur(long cod)
        {
            string s = "";
            foreach (Figur figur in listBox3.Items)
            {
                if (figur.cod == cod)
                {
                    s = figur.name;
                }
            }
            return s;
        }


        //****************************************
        // Загрузка проекта и фигуры
        //****************************************
        void SelectFigurPr()
        {
            //Установим доступность элементов управления
            AccessControlProject(0);
            //Начальное значение полей
            SetFieldProject();

            if (listBox2.Items.Count > 0)
            {
                Project project = (Project)listBox2.Items[listBox2.SelectedIndex];
                //Заполним поля
                textBox2.Text = project.name;
                numericUpDown1.Value = project.listx;
                numericUpDown2.Value = project.listy;
                numericUpDown3.Value = project.step_p;
                numericUpDown4.Value = project.step_a;
                //Прочитаем из базы список фигур
                ArrayList arrListFigurPr = dateBase.ReadListFigurProject(project);
                foreach (FigurPr figurpr in arrListFigurPr)
                {
                    dataGridView1.Rows.Add();
                    dataGridView1["cod", dataGridView1.Rows.Count - 1].Value = figurpr.codfigur;
                    dataGridView1["name", dataGridView1.Rows.Count - 1].Value = NameFigur(figurpr.codfigur);
                    dataGridView1["col", dataGridView1.Rows.Count - 1].Value = figurpr.col;
                }
            }
        }

        //****************************************
        // Сохранить проект
        //****************************************
        void SaveProect()
        {
            Project project;

            if (!newProect)
            {
                //Это редактируем проект
                project = (Project)listBox2.Items[listBox2.SelectedIndex];
                project.name = textBox2.Text;
                project.listx = (long)numericUpDown1.Value;
                project.listy = (long)numericUpDown2.Value;
                project.step_p = (long)numericUpDown3.Value;
                project.step_a = (long)numericUpDown4.Value;
                //Удалим фигуры проекта
                dateBase.DeleteFigurProject(project);
                //Запишем проект
                dateBase.WriteProject(project);
            }
            else
            {
                //Это новый
                project = new Project(0, textBox2.Text, (long)numericUpDown1.Value, (long)numericUpDown2.Value,
                                                        (long)numericUpDown3.Value, (long)numericUpDown4.Value);
                //Запишем новый проект
                project.cod = dateBase.AddProgect(project);
            }
            //Запишем фигуры проекта
            if (project.cod > 0)
            {
                ArrayList arrList = new ArrayList();
                for (int i = 0; i < dataGridView1.Rows.Count; ++i)
                {
                    arrList.Add(new FigurPr(project.cod, (long)dataGridView1[0, i].Value, long.Parse(dataGridView1[2, i].Value.ToString())));
                }
                dateBase.AddFigurProject(arrList);
            }
        }

        //****************************************
        // Удалить проект
        //****************************************
        void DeleteProject()
        {
            if (listBox2.SelectedIndex > -1)
            {
                Project project = (Project)listBox2.Items[listBox2.SelectedIndex];
                //Удалим проект
                dateBase.DeleteProject(project);
            }
        }

        //****************************************
        // Новый проект
        //****************************************
        private void button6_Click(object sender, EventArgs e)
        {
            if (!editProect)
            {
                editProect = true;
                newProect = true;
                //Установим доступность элементов управления
                AccessControlProject(1);
                //Начальное значение полей
                SetFieldProject();
                textBox2.Text = "Новый проект";
            }
        }

        //****************************************
        // Добавить фигуру в таблицу
        //****************************************
        private void button12_Click(object sender, EventArgs e)
        {
            if (listBox3.SelectedIndex > -1)
            {
                Figur figur = (Figur)listBox3.Items[listBox3.SelectedIndex];

                //Проверим фигура есть в таблице
                long cod;
                for (int i = 0; i < dataGridView1.Rows.Count; ++i)
                {
                    cod = (long)dataGridView1[0, i].Value;
                    if (cod == figur.cod)
                    {
                        return;
                    }
                }
                //Добавим строчку
                dataGridView1.Rows.Add();
                dataGridView1["cod", dataGridView1.Rows.Count - 1].Value = figur.cod;
                dataGridView1["name", dataGridView1.Rows.Count - 1].Value = NameFigur(figur.cod);
                dataGridView1["col", dataGridView1.Rows.Count - 1].Value = 1;
                dataGridView1.CurrentCell = dataGridView1[1, dataGridView1.Rows.Count - 1];
            }
        }

        //****************************************
        // Удалить фигуру из таблицы
        //****************************************
        private void button11_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewCell cell in dataGridView1.SelectedCells)
            {
                dataGridView1.Rows.RemoveAt(cell.RowIndex);
            }
        }

        //****************************************
        // Отменить редактирование
        //****************************************
        private void button8_Click(object sender, EventArgs e)
        {
            if (editProect)
            {
                //Установим признаки
                editProect = false;
                newProect = false;
                //Установим доступность элементов управления
                AccessControlProject(0);
                //Начальное значение полей
                SetFieldProject();
                //Загрузить проект
                LoadProjects();
            }
        }

        //****************************************
        // Начать редактирование
        //****************************************
        private void button9_Click(object sender, EventArgs e)
        {
            if (!editProect)
            {
                //Установим признаки
                editProect = true;
                newProect = false;
                //Установим доступность элементов управления
                AccessControlProject(1);
            }
        }

        //****************************************
        // Сохранить
        //****************************************
        private void button10_Click(object sender, EventArgs e)
        {
            if (editProect)
            {
                //Выведим сообщение пользователю
                DialogResult result = MessageBox.Show("Сохранить данные?", "Внимание!", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (result == DialogResult.Yes)
                {
                    SaveProect();
                }
                //Установим признаки
                editProect = false;
                newProect = false;
                //Установим доступность элементов управления
                AccessControlProject(0);
                //Начальное значение полей
                SetFieldProject();
                //Загрузить проект
                LoadProjects();
            }
        }

        //****************************************
        // При выборе проекта
        //****************************************
        private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            SelectFigurPr();
        }

        //****************************************
        // Удалить проект
        //****************************************
        private void button7_Click(object sender, EventArgs e)
        {
            if (!editProect)
            {
                DialogResult result = MessageBox.Show("Удалить проект?", "Внимание!", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (result == DialogResult.Yes)
                {
                    DeleteProject();
                    LoadProjects();
                }
            }
        }

        //****************************************
        // Контроль ввода числа в таблице
        //****************************************
        private void dataGridView1_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            //Контролируем только третью колонку
            if (dataGridView1.CurrentCell.ColumnIndex == 2)
            {
                TextBox tb = (TextBox)e.Control;
                tb.KeyPress += new KeyPressEventHandler(tb_KeyPress);
            }
            else
            {
                TextBox tb = (TextBox)e.Control;
                tb.KeyPress -= tb_KeyPress;
            }
        }

        //****************************************
        // Контроль ввода числа в таблице
        //****************************************
        void tb_KeyPress(object sender, KeyPressEventArgs e)
        {
            //Пропускаем только цифры
            if (!Char.IsNumber(e.KeyChar))
            {
                if ((e.KeyChar != (char)Keys.Back) || (e.KeyChar != (char)Keys.Delete))
                {
                    e.Handled = true;
                }
            }
        }

        //*********************************************************************************************
        //               РАСЧЕТ
        //*********************************************************************************************

        //****************************************
        // Загрузить проекты
        //****************************************
        void LoadProjects2()
        {
            //Очистить лист
            comboBox1.Items.Clear();
            //Прочитаем из базы список фигур
            ArrayList arrList = dateBase.ReadListProject();
            //Заполним список
            comboBox1.BeginUpdate();
            foreach (Project project in arrList)
            {
                comboBox1.Items.Add(project);
            }
            comboBox1.EndUpdate();
            //Подсветим первый элемент
            if (comboBox1.Items.Count > 0)
            {
                comboBox1.SelectedIndex = 0;
            }
        }

        //****************************************
        // Запустить расчет проекта
        //****************************************
        private void button13_Click(object sender, EventArgs e)
        {
            if ((statusProject == 0) || (statusProject == 9) || (statusProject == 10))
            {
                //Проверим выбран ли проект
                if (comboBox1.SelectedIndex < 0)
                {
                    DialogResult result = MessageBox.Show("Проект не выбран.", "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                //Прочитаем параметры проекта
                Project project = (Project)comboBox1.Items[comboBox1.SelectedIndex];
                //Прочитаем из базы список фигур
                ArrayList arrListFigurPr = dateBase.ReadListFigurProject(project);
                //Если в проекте нет фигур выходим
                if (arrListFigurPr.Count == 0)
                {
                    DialogResult result = MessageBox.Show("В проект нет фигур.", "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                //Если в проекте несколько фигур выходим
                if (arrListFigurPr.Count > 1)
                {
                    DialogResult result = MessageBox.Show("Для данного алгоритма в проекте должна быть только одна фигура.", "Внимание!",
                                                          MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                //Получим код фигуры
                Figur figur = new Figur();
                figur.cod = ((FigurPr)arrListFigurPr[0]).codfigur;

                //Получим вершины фигуры
                ArrayList arrListVertex = dateBase.ReadListVertexFigur(figur);

                //Создадим структуру для передачи в процедуру расчета
                ParamForGo paramForGo = new ParamForGo(project, figur, arrListVertex);

                //Запустим таймер для отображения хода выполнения
                timer1.Enabled = true;

                //Создаем новый поток и в нем запускаем расчет StartFullCalc(paramForGo)
                Thread thread = new Thread(new ParameterizedThreadStart(StartFullCalc));
                //project - это пареметр метода StartFullCalc
                thread.Start(paramForGo);
            }
        }

        //*****************************************
        //Запускает полный расчет 
        //*****************************************
        private void StartFullCalc(object paramForGo)
        {
            if ((statusProject == 0) || (statusProject == 9) || (statusProject == 10))
            {
                //Установим состояние расчета в 1
                statusProject = 0;
                //Получим параметры
                ParamForGo param = (ParamForGo)paramForGo;

                //Если не было прерывания пользователя продолжим
                if (statusProject < 10)
                {
                    statusProject++;
                    //*****************************************************************
                    //Создадим объект для рассчета для этапа 1 треугольник по вершинам
                    //*****************************************************************
                    algorithm1 = new Algorithm();
                    algorithm1.poligon1.completed = true;
                    algorithm1.poligon2.completed = true;
                    //Загрузим вершины многоугольника
                    foreach (Vertex vertex in param.arrListVertex)
                    {
                        algorithm1.poligon1.AddVertex(new PointF(vertex.x, vertex.y));
                        algorithm1.poligon2.AddVertex(new PointF(vertex.x, vertex.y));
                    }
                    //Установим параметры точности расчета
                    algorithm1.deltaStep = param.project.step_p;
                    algorithm1.deltaAngle = param.project.step_a;
                    //Для вывода в центр pictureBox
                    algorithm1.formSizeX = pictureBox3.Width;
                    algorithm1.formSizeY = pictureBox3.Height;
                    //Тип расчета
                    algorithm1.typeRotation = 1;
                    algorithm1.typeRascheta = 1;
                    //Запустим расчет Этап 1;
                    algorithm1.StartCalc();
                }

                //Если не было прерывания пользователя продолжим
                if (statusProject < 10)
                {
                    statusProject++;
                    //*****************************************************************
                    //Создадим объект для рассчета для этапа 2 прямоугольник 1 фигура
                    //*****************************************************************
                    algorithm2 = new Algorithm();
                    algorithm2.poligon1.completed = true;
                    algorithm2.poligon2.completed = true;
                    //Загрузим вершины многоугольника
                    foreach (Vertex vertex in param.arrListVertex)
                    {
                        algorithm2.poligon1.AddVertex(new PointF(vertex.x, vertex.y));
                        algorithm2.poligon2.AddVertex(new PointF(vertex.x, vertex.y));
                    }
                    //Установим параметры точности расчета
                    algorithm2.deltaStep = param.project.step_p;
                    algorithm2.deltaAngle = param.project.step_a;
                    //Для вывода в центр pictureBox
                    algorithm2.formSizeX = pictureBox3.Width;
                    algorithm2.formSizeY = pictureBox3.Height;
                    //Тип расчета
                    algorithm2.typeRotation = 0;
                    algorithm2.typeRascheta = 2;
                    //Запустим расчет Этап 2;
                    algorithm2.StartCalc();
                }

                //Если не было прерывания пользователя продолжим
                if (statusProject < 10)
                {
                    statusProject++;
                    //*****************************************************************
                    //Создадим объект для рассчета для этапа 3 прямоугольник 2 фигуры
                    //*****************************************************************
                    algorithm3 = new Algorithm();
                    algorithm3.poligon1.completed = true;
                    algorithm3.poligon2.completed = true;
                    //Загрузим вершины многоугольника
                    foreach (Vertex vertex in param.arrListVertex)
                    {
                        algorithm3.poligon1.AddVertex(new PointF(vertex.x, vertex.y));
                        algorithm3.poligon2.AddVertex(new PointF(vertex.x, vertex.y));
                    }
                    //Установим параметры точности расчета
                    algorithm3.deltaStep = param.project.step_p;
                    algorithm3.deltaAngle = param.project.step_a;
                    //Для вывода в центр pictureBox
                    algorithm3.formSizeX = pictureBox3.Width;
                    algorithm3.formSizeY = pictureBox3.Height;
                    //Тип расчета
                    algorithm3.typeRotation = 0;
                    algorithm3.typeRascheta = 3;
                    //Запустим расчет Этап 3;
                    algorithm3.StartCalc();
                }

                //Если не было прерывания пользователя продолжим
                if (statusProject < 10)
                {
                    statusProject++;
                    //*****************************************************************************
                    //Создадим объект для рассчета для этапа 4 объединение из этапа 1 без поворота
                    //*****************************************************************************
                    //Объединим фигуры из этапа 0
                    unionPoligon1 = Poligon.UnionPoligon(ref algorithm1.poligon1.arrVertex, ref algorithm1.bestPoligon2);
                    algorithm4 = new Algorithm();
                    algorithm4.poligon1.completed = true;
                    algorithm4.poligon2.completed = true;
                    //Это составная фигура
                    //Загрузим вершины исходных фмгур для прорисовки
                    foreach (PointF vertex in algorithm1.poligon1.arrVertex)
                    {
                        algorithm4.component11.AddVertex(new PointF(vertex.X, vertex.Y));
                        algorithm4.component21.AddVertex(new PointF(vertex.X, vertex.Y));
                    }
                    foreach (PointF vertex in algorithm1.bestPoligon2)
                    {
                        algorithm4.component12.AddVertex(new PointF(vertex.X, vertex.Y));
                        algorithm4.component22.AddVertex(new PointF(vertex.X, vertex.Y));
                    }
                    //Загрузим вершины многоугольника
                    foreach (PointF vertex in unionPoligon1.arrVertex)
                    {
                        algorithm4.poligon1.AddVertex(new PointF(vertex.X, vertex.Y));
                        algorithm4.poligon2.AddVertex(new PointF(vertex.X, vertex.Y));
                    }

                    //Установим параметры точности расчета
                    algorithm4.deltaStep = param.project.step_p;
                    algorithm4.deltaAngle = param.project.step_a;
                    //Для вывода в центр pictureBox
                    algorithm4.formSizeX = pictureBox3.Width;
                    algorithm4.formSizeY = pictureBox3.Height;
                    //Тип расчета
                    algorithm4.typeRotation = 0;
                    algorithm4.typeRascheta = 3;
                    //Запустим расчет Этап 6;
                    algorithm4.StartCalc();
                }

                //Если не было прерывания пользователя продолжим
                if (statusProject < 10)
                {
                    statusProject++;
                    //*****************************************************************************
                    //Создадим объект для рассчета для этапа 5 объединение из этапа 1 с поворотом
                    //*****************************************************************************
                    //Объединим фигуры из этапа 0
                    unionPoligon2 = Poligon.UnionPoligon(ref algorithm1.poligon1.arrVertex, ref algorithm1.bestPoligon2);
                    algorithm5 = new Algorithm();
                    algorithm5.poligon1.completed = true;
                    algorithm5.poligon2.completed = true;
                    //Это составная фигура
                    //Загрузим вершины исходных фмгур для прорисовки
                    foreach (PointF vertex in algorithm1.poligon1.arrVertex)
                    {
                        algorithm5.component11.AddVertex(new PointF(vertex.X, vertex.Y));
                        algorithm5.component21.AddVertex(new PointF(vertex.X, vertex.Y));
                    }
                    foreach (PointF vertex in algorithm1.bestPoligon2)
                    {
                        algorithm5.component12.AddVertex(new PointF(vertex.X, vertex.Y));
                        algorithm5.component22.AddVertex(new PointF(vertex.X, vertex.Y));
                    }
                    //Загрузим вершины многоугольника
                    foreach (PointF vertex in unionPoligon2.arrVertex)
                    {
                        algorithm5.poligon1.AddVertex(new PointF(vertex.X, vertex.Y));
                        algorithm5.poligon2.AddVertex(new PointF(vertex.X, vertex.Y));
                    }

                    //Установим параметры точности расчета
                    algorithm5.deltaStep = param.project.step_p;
                    algorithm5.deltaAngle = param.project.step_a;
                    //Для вывода в центр pictureBox
                    algorithm5.formSizeX = pictureBox3.Width;
                    algorithm5.formSizeY = pictureBox3.Height;
                    //Тип расчета
                    algorithm5.typeRotation = 1;
                    algorithm5.typeRascheta = 3;
                    //Запустим расчет Этап 7;
                    algorithm5.StartCalc();
                }

                //Если не было прерывания пользователя продолжим
                if (statusProject < 10)
                {
                    statusProject = 9;
                }
            }
        }

        //****************************************
        // Остановить расчет
        //****************************************
        private void button14_Click(object sender, EventArgs e)
        {
            //Чтобы остановить просто устанавливаем признаки остановки
            if (algorithm1 != null)
            {
                algorithm1.stop = true;
            }
            if (algorithm2 != null)
            {
                algorithm2.stop = true;
            }
            if (algorithm3 != null)
            {
                algorithm3.stop = true;
            }
            if (algorithm4 != null)
            {
                algorithm4.stop = true;
            }
            if (algorithm5 != null)
            {
                algorithm5.stop = true;
            }
            statusProject = 10;
        }

        //****************************************
        // Обработчик таймера для отрисовки
        //****************************************
        private void timer1_Tick(object sender, EventArgs e)
        {
            //Если расчет завершен остановим таймер
            if ((statusProject == 9) || (statusProject == 10))
            {
                timer1.Enabled = false;

                if (statusProject == 9)
                {

                    //Выберем лучшую 
                    if ((algorithm2.percentPack >= algorithm3.percentPack) &&
                       (algorithm2.percentPack >= algorithm4.percentPack) &&
                       (algorithm2.percentPack >= algorithm5.percentPack))
                    {
                        gCalc.Clear(Color.Transparent);
                        algorithm2.DrawBest(gCalc, penРerimetr, penPoligon);
                        pictureBox3.Invalidate();
                        label12.Text = algorithm2.percentPack.ToString();
                        CreateCartRascroy(algorithm2, 1);
                    }
                    else
                    {
                        if ((algorithm3.percentPack >= algorithm2.percentPack) &&
                           (algorithm3.percentPack >= algorithm4.percentPack) &&
                           (algorithm3.percentPack >= algorithm5.percentPack))
                        {
                            gCalc.Clear(Color.Transparent);
                            algorithm3.DrawBest(gCalc, penРerimetr, penPoligon);
                            pictureBox3.Invalidate();
                            label12.Text = algorithm3.percentPack.ToString();
                            CreateCartRascroy(algorithm3, 1);
                        }
                        else
                        {
                            if ((algorithm4.percentPack >= algorithm2.percentPack) &&
                               (algorithm4.percentPack >= algorithm3.percentPack) &&
                               (algorithm4.percentPack >= algorithm5.percentPack))
                            {
                                gCalc.Clear(Color.Transparent);
                                algorithm4.RotationComposit();
                                algorithm4.DrawBestComposit(gCalc, penРerimetr, penPoligon);
                                pictureBox3.Invalidate();
                                label12.Text = algorithm4.percentPack.ToString();
                                CreateCartRascroy(algorithm4, 2);
                            }
                            else
                            {
                                gCalc.Clear(Color.Transparent);
                                algorithm5.RotationComposit();
                                algorithm5.DrawBestComposit(gCalc, penРerimetr, penPoligon);
                                pictureBox3.Invalidate();
                                label12.Text = algorithm5.percentPack.ToString();
                                CreateCartRascroy(algorithm5, 2);
                            }

                        }
                    }
                }
            }
            //Нарисуем все
            DrawAll();
        }

        //****************************************
        // Рисует процесс расчета
        //****************************************
        void DrawAll()
        {
            //Выведем состояние
            if (statusProject == 10)
            {
                label10.Text = "Расчет остановлен.";
                progressBar1.Value = 100;
            }
            if (statusProject == 9)
            {
                label10.Text = "Расчет завершен.";
                progressBar1.Value = 100;
            }
            if (statusProject < 9)
            {
                label10.Text = "Выполняется расчет. Этап " + statusProject.ToString();
            }

            //Нарисуем состояние расчета
            switch (statusProject)
            {
                case 9:
                    progressBar1.Value = 100;
                    break;
                case 1:
                    gCalc.Clear(Color.Transparent);
                    algorithm1.DrawBest(gCalc, penРerimetr, penPoligon);
                    pictureBox3.Invalidate();
                    progressBar1.Value = algorithm1.progBar;
                    label12.Text = algorithm1.percentPack.ToString();
                    break;
                case 2:
                    gCalc.Clear(Color.Transparent);
                    algorithm2.DrawBest(gCalc, penРerimetr, penPoligon);
                    pictureBox3.Invalidate();
                    progressBar1.Value = algorithm2.progBar;
                    label12.Text = algorithm2.percentPack.ToString();
                    break;
                case 3:
                    gCalc.Clear(Color.Transparent);
                    algorithm3.DrawBest(gCalc, penРerimetr, penPoligon);
                    pictureBox3.Invalidate();
                    progressBar1.Value = algorithm3.progBar;
                    label12.Text = algorithm3.percentPack.ToString();
                    break;
                case 4:
                    gCalc.Clear(Color.Transparent);
                    algorithm4.DrawBest(gCalc, penРerimetr, penPoligon);
                    pictureBox3.Invalidate();
                    progressBar1.Value = algorithm4.progBar;
                    label12.Text = algorithm4.percentPack.ToString();
                    break;
                case 5:
                    gCalc.Clear(Color.Transparent);
                    algorithm5.DrawBest(gCalc, penРerimetr, penPoligon);
                    pictureBox3.Invalidate();
                    progressBar1.Value = algorithm5.progBar;
                    label12.Text = algorithm5.percentPack.ToString();
                    break;
            }
        }

        //****************************************
        // Создать карту раскроя
        //****************************************
        void CreateCartRascroy(Algorithm algoritm, int typeRascroy)
        {
            int listX;    //Ширина листа
            int listY;    //Высота листа
            int rectX;    //Ширина прямоугольника
            int rectY;    //Высота прямоугольника
            int maxNum;   //Максимальное кол-во элементов
            int maxSX;    //Максимальное кол-во элементов
            int maxSY;    //Максимальное кол-во элементов
            int S;        //Служебные переменные
            int l;        //Служебные переменные
            int j;        //Служебные переменные

            int maxiX = 0; //Количество фигур по строкам и столбцам
            int maxjX = 0; //Количество фигур по строкам и столбцам
            int maxiY = 0; //Количество фигур по строкам и столбцам
            int maxjY = 0; //Количество фигур по строкам и столбцам

            int colFigur = 0;

            float kImage; //Коэффициент уменьшения изображения

            string writePath = @"result.txt";  //Имя файла
            StreamWriter sw = null;           //Поток  для файла


            DialogResult result = MessageBox.Show("Расчет завершен. Создать карту раскроя?", "Внимание!", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (result == DialogResult.Yes)
            {
                //Откроем файл для записи
                try
                {
                    sw = new StreamWriter(writePath, false, System.Text.Encoding.Default);
                }
                catch
                {
                }

                //Прочитаем параметры проекта
                Project project = (Project)comboBox1.Items[comboBox1.SelectedIndex];
                listX = (int)project.listx;
                listY = (int)project.listy;
                //Параметры прямоугольника
                rectX = (int)Math.Round(algoritm.xMaxRect - algoritm.xMinRect);
                rectY = (int)Math.Round(algoritm.yMaxRect - algoritm.yMinRect);
                //Запишем в файл
                try
                {
                    sw.WriteLine("Ширина листа:" + listX.ToString());
                    sw.WriteLine("Высота листа:" + listY.ToString());
                    sw.WriteLine("");
                    sw.WriteLine("Координаты фигур:");
                }
                catch
                {
                }
                //Максимальное количество на листе по стороне X
                maxSX = 0;
                maxNum = (int)Math.Max(Math.Floor((double)(listX / rectX)), Math.Floor((double)(listX / rectY)));
                for (int i = 0; i <= maxNum; i++)
                {
                    l = (maxNum - i) * rectX;
                    j = 0;
                    while ((l + rectY) <= listX)
                    {
                        l = l + rectY;
                        j++;
                    }
                    if (l <= listX)
                    {
                        S = (maxNum - i) * rectX * rectY * (int)(Math.Floor((double)(listY / rectY)));
                        S = S + j * rectY * rectX * (int)(Math.Floor((double)(listY / rectX)));
                        if (S > maxSX)
                        {
                            maxSX = S;
                            maxiX = maxNum - i;
                            maxjX = j;
                        }
                    }
                }
                //Максимальное количество на листе по стороне Y
                maxSY = 0;
                maxNum = (int)Math.Max(Math.Floor((double)(listY / rectX)), Math.Floor((double)(listY / rectY)));
                for (int i = 0; i <= maxNum; i++)
                {
                    l = (maxNum - i) * rectX;
                    j = 0;
                    while ((l + rectY) <= listY)
                    {
                        l = l + rectY;
                        j++;
                    }
                    if (l <= listY)
                    {
                        S = (maxNum - i) * rectX * rectY * (int)(Math.Floor((double)(listX / rectY)));
                        S = S + j * rectY * rectX * (int)(Math.Floor((double)(listX / rectX)));
                        if (S > maxSY)
                        {
                            maxSY = S;
                            maxiY = maxNum - i;
                            maxjY = j;
                        }
                    }
                }
                //Нарисуем все
                gCalc.Clear(Color.Transparent);
                kImage = Math.Max((float)listX / (algoritm.formSizeX - 10), (float)listY / (algoritm.formSizeY - 10));
                float startX = algoritm.formSizeX / 2 - listX / (2 * kImage);
                float startY = algoritm.formSizeY / 2 - listY / (2 * kImage);
                float dX = listX / kImage;
                float dY = listY / kImage;
                gCalc.DrawRectangle(penPage, startX, startY, dX, dY);
                if (maxSX >= maxSY)
                { 
                    //Рисуем по стороне X
                    //Рисуем стороной X фигуры
                    for (int i = 1; i <= maxiX; i++)
                    {
                        int colStr = (int)(Math.Floor((double)(listY / rectY)));
                        for (int k = 1; k <= colStr; k++)
                        {
                            //gCalc.DrawRectangle(penPoligon, startX + (i - 1) * rectX / kImage,
                            //                                startY + (k - 1) * rectY / kImage,
                            //                                rectX / kImage,
                            //                                rectY / kImage);
                            if (typeRascroy == 1)
                            {
                                algoritm.DrawFigurInCard(gCalc, penPoligon, 1 / kImage,
                                                   startX + (i - 1) * rectX / kImage,
                                                   startY + (k - 1) * rectY / kImage,
                                                   0, sw);
                            }
                            else
                            {
                                algoritm.DrawFigurInCardComposit(gCalc, penPoligon, 1 / kImage,
                                                   startX + (i - 1) * rectX / kImage,
                                                   startY + (k - 1) * rectY / kImage,
                                                   0, sw);
                            }
                            colFigur++;
                        }
                    }
                    //Рисуем стороной Y фигуры
                    for (int i = 1; i <= maxjX; i++)
                    {
                        int colStr = (int)(Math.Floor((double)(listY / rectX)));
                        for (int k = 1; k <= colStr; k++)
                        {
                            //gCalc.DrawRectangle(penPoligon, startX + maxiX * rectX / kImage + (i - 1) * rectY/ kImage,
                            //                                startY + (k - 1) * rectX / kImage,
                            //                                rectY / kImage,
                            //                                rectX / kImage);
                            if (typeRascroy == 1)
                            {
                                algoritm.DrawFigurInCard(gCalc, penPoligon, 1 / kImage,
                                               startX + maxiX * rectX / kImage + (i - 1) * rectY / kImage,
                                               startY + (k - 1) * rectX / kImage,
                                               1, sw);
                            }
                            else
                            {
                                algoritm.DrawFigurInCardComposit(gCalc, penPoligon, 1 / kImage,
                                               startX + maxiX * rectX / kImage + (i - 1) * rectY / kImage,
                                               startY + (k - 1) * rectX / kImage,
                                               1, sw);
                            }
                            colFigur++;
                        }
                    }
                }
                else
                {
                    //Рисуем по стороне Y
                    //Рисуем стороной X фигуры
                    for (int i = 1; i <= maxiY; i++)
                    {
                        int colCol = (int)(Math.Floor((double)(listX / rectY)));
                        for (int k = 1; k <= colCol; k++)
                        {
                            //gCalc.DrawRectangle(penPoligon, startX + (k - 1) * rectY / kImage,
                            //                                startY + (i - 1) * rectX / kImage,
                            //                                rectY / kImage,
                            //                                rectX / kImage);
                            if (typeRascroy == 1)
                            {
                                algoritm.DrawFigurInCard(gCalc, penPoligon, 1 / kImage,
                                               startX + (k - 1) * rectY / kImage,
                                               startY + (i - 1) * rectX / kImage,
                                               1, sw);
                            }
                            else
                            {
                                algoritm.DrawFigurInCardComposit(gCalc, penPoligon, 1 / kImage,
                                               startX + (k - 1) * rectY / kImage,
                                               startY + (i - 1) * rectX / kImage,
                                               1, sw);
                            }
                            colFigur++;
                        }
                    }
                    //Рисуем стороной Y фигуры
                    for (int i = 1; i <= maxjY; i++)
                    {
                        int colCol = (int)(Math.Floor((double)(listX / rectX)));
                        for (int k = 1; k <= colCol; k++)
                        {
                            //gCalc.DrawRectangle(penPoligon, startX + (k - 1) * rectX / kImage,
                            //                                startY + maxiY * rectX / kImage + (i-1) * rectY/ kImage,
                            //                                rectX / kImage,
                            //                                rectY / kImage);
                            if (typeRascroy == 1)
                            {
                                algoritm.DrawFigurInCard(gCalc, penPoligon, 1 / kImage,
                                               startX + (k - 1) * rectX / kImage,
                                               startY + maxiY * rectX / kImage + (i - 1) * rectY / kImage,
                                               0, sw);
                            }
                            else
                            {
                                algoritm.DrawFigurInCardComposit(gCalc, penPoligon, 1 / kImage,
                                               startX + (k - 1) * rectX / kImage,
                                               startY + maxiY * rectX / kImage + (i - 1) * rectY / kImage,
                                               0, sw);
                            }
                            colFigur++;
                        }
                    }
                }

                //Расчитаем и выведем процент упаковки
                label12.Text = ((rectX * rectY * colFigur * algoritm.percentPack) / (listX * listY)).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);

                //Обновим графику
                pictureBox3.Invalidate();

                //Запишем и закроем файл
                try
                {
                    sw.WriteLine();
                    sw.WriteLine();
                    sw.WriteLine("Процент упаковки:" + label12.Text);
                    sw.Close();
                }
                catch
                {
                }

                //Откроем файл в блокноте или в другой программе
                System.Diagnostics.Process.Start(writePath);
            }
        }

        private void button34_Click(object sender, EventArgs e)
        {

        }

        //*********************************************************************************************
        //*********************************************************************************************
        //**************        ЭКСПЕРТНАЯ СИСИЕМА      ***********************************************
        //*********************************************************************************************
        //*********************************************************************************************
        private void button15_Click(object sender, EventArgs e)
        {
            tabControl2.SelectTab(1);
        }

        private void button16_Click(object sender, EventArgs e)
        {
            tabControl2.SelectTab(2);
        }

        private void button19_Click(object sender, EventArgs e)
        {
            tabControl2.SelectTab(3);
        }

        private void button21_Click(object sender, EventArgs e)
        {
            tabControl2.SelectTab(4);
        }

        private void button23_Click(object sender, EventArgs e)
        {
            tabControl2.SelectTab(5);
        }

        private void button22_Click(object sender, EventArgs e)
        {
            tabControl2.SelectTab(6);
        }

        private void button20_Click(object sender, EventArgs e)
        {
            tabControl2.SelectTab(7);
        }

        private void button18_Click(object sender, EventArgs e)
        {
            tabControl2.SelectTab(8);
        }

        private void button28_Click(object sender, EventArgs e)
        {
            tabControl2.SelectTab(9);
        }

        private void button27_Click(object sender, EventArgs e)
        {
            tabControl2.SelectTab(10);
        }

        private void button17_Click(object sender, EventArgs e)
        {
            tabControl2.SelectTab(11);
        }

        private void button32_Click(object sender, EventArgs e)
        {
            tabControl2.SelectTab(12);
        }

        private void button34_Click_1(object sender, EventArgs e)
        {
            tabControl2.SelectTab(13);
        }

        private void button35_Click(object sender, EventArgs e)
        {
            tabControl2.SelectTab(14);
        }

        private void button33_Click(object sender, EventArgs e)
        {
            tabControl2.SelectTab(15);
        }

        private void button31_Click(object sender, EventArgs e)
        {
            tabControl2.SelectTab(16);
        }

        private void button40_Click(object sender, EventArgs e)
        {
            tabControl2.SelectTab(17);
        }

        private void button42_Click(object sender, EventArgs e)
        {
            tabControl2.SelectTab(18);
        }

        private void button45_Click(object sender, EventArgs e)
        {
            tabControl2.SelectTab(19);
        }

        private void button43_Click(object sender, EventArgs e)
        {
            tabControl2.SelectTab(20);
        }

        private void button44_Click(object sender, EventArgs e)
        {
            tabControl2.SelectTab(21);
        }

        private void button41_Click(object sender, EventArgs e)
        {
            tabControl2.SelectTab(22);
        }

        private void button51_Click(object sender, EventArgs e)
        {
            tabControl2.SelectTab(23);
        }

        private void button49_Click(object sender, EventArgs e)
        {
            tabControl2.SelectTab(24);
        }

        private void button50_Click(object sender, EventArgs e)
        {
            tabControl2.SelectTab(25);
        }

        private void button39_Click(object sender, EventArgs e)
        {
            tabControl2.SelectTab(25);
        }

        private void button24_Click(object sender, EventArgs e)
        {
            tabControl2.SelectTab(0);
        }

        private void button25_Click(object sender, EventArgs e)
        {
            tabControl2.SelectTab(0);
        }

        private void button26_Click(object sender, EventArgs e)
        {
            tabControl2.SelectTab(0);
        }

        private void button29_Click(object sender, EventArgs e)
        {
            tabControl2.SelectTab(0);
        }

        private void button30_Click(object sender, EventArgs e)
        {
            tabControl2.SelectTab(0);
        }

        private void button36_Click(object sender, EventArgs e)
        {
            tabControl2.SelectTab(0);
        }

        private void button37_Click(object sender, EventArgs e)
        {
            tabControl2.SelectTab(0);
        }

        private void button38_Click(object sender, EventArgs e)
        {
            tabControl2.SelectTab(0);
        }

        private void button46_Click(object sender, EventArgs e)
        {
            tabControl2.SelectTab(0);
        }

        private void button47_Click(object sender, EventArgs e)
        {
            tabControl2.SelectTab(0);
        }

        private void button48_Click(object sender, EventArgs e)
        {
            tabControl2.SelectTab(0);
        }

        private void button52_Click(object sender, EventArgs e)
        {
            tabControl2.SelectTab(0);
        }

        private void button53_Click(object sender, EventArgs e)
        {
            tabControl2.SelectTab(0);
        }

        private void button54_Click(object sender, EventArgs e)
        {
            tabControl2.SelectTab(0);
        }

        private void button55_Click(object sender, EventArgs e)
        {
            tabControl2.SelectTab(0);
        }
    }
}
