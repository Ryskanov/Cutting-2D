using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.Data;
using System.IO;
using System.Collections;


namespace RyskanovDiplom
{
    //Структура для фигуры
    public struct Figur
    {
        public long cod;
        public string name;
        public Figur(long pcod, string pname)
        {
            cod = pcod;
            name = pname;
        }
        public override string ToString() => $"{name}";
    }

    //Структура для вершин фигуры
    public struct Vertex
    {
        public long cod;
        public long num;
        public float x;
        public float y;
        public Vertex(long pcod, long pnum, float px, float py)
        {
            cod = pcod;
            num = pnum;
            x = px;
            y = py;
        }
    }

    //Структура для проекта
    public struct Project
    {
        public long cod;
        public string name;
        public long listx;
        public long listy;
        public long step_p;
        public long step_a;
        public Project(long pcod, string pname, long plistx, long plisty, long pstep_p, long pstep_a)
        {
            cod = pcod;
            name = pname;
            listx = plistx;
            listy = plisty;
            step_p = pstep_p;
            step_a = pstep_a;
        }
        public override string ToString() => $"{name}";
    }

    //Структура для фигуры проекта
    public struct FigurPr
    {
        public long cod;
        public long codfigur;
        public long col;
        public FigurPr(long pcod, long pcodfigur, long pcol)
        {
            cod = pcod;
            codfigur = pcodfigur;
            col = pcol;
        }
    }


    class DB
    {
        SQLiteConnection connect;
        SQLiteCommand cmd;

        //*******************************
        // Создание файла базы данных
        //*******************************
        public int CreateDB()
        {
            int ret = 0;

            if (!File.Exists(@"CursDB.db"))
            {

                try
                {
                    SQLiteConnection.CreateFile(@"CursDB.db");
                    connect = new SQLiteConnection("Data Source=CursDB.db; Version=3;");
                    connect.Open();
                    cmd = connect.CreateCommand();

                    //Создать таблицу фигуры 
                    cmd.CommandText = "CREATE TABLE figur("
                                    + "cod INTEGER PRIMARY KEY AUTOINCREMENT, " // Код фигуры
                                    + "name TEXT)";                            // Наименование фигуры
                    cmd.ExecuteNonQuery();

                    //Создать таблицу вершин фигур
                    cmd.CommandText = "CREATE TABLE vertex("
                                    + "cod INTEGER, "       // Код фигуры
                                    + "num INTEGER, "       // Номер вершины
                                    + "x REAL, "            // X - координата вершины
                                    + "y REAL)";            // y - координата вершины
                    cmd.ExecuteNonQuery();

                    //Создать таблицу проектов
                    cmd.CommandText = "CREATE TABLE project("
                                    + "cod INTEGER PRIMARY KEY AUTOINCREMENT, " // Код проекта
                                    + "name TEXT, "                             // Имя проекта
                                    + "listx INTEGER, "                         // X - ширина листа
                                    + "listy INTEGER, "                         // X - высота листа
                                    + "step_p INTEGER, "                        // шаг по периметру
                                    + "step_a INTEGER)";                        // шаг по углу
                    cmd.ExecuteNonQuery();

                    //Создать таблицу фигуры по проекту
                    cmd.CommandText = "CREATE TABLE figurpr("
                                    + "cod INTEGER , "                          // Код проекта
                                    + "codfigur INTEGER , "                     // Код проекта
                                    + "col INTEGER )";                          // Количество
                    cmd.ExecuteNonQuery();

                    connect.Close();
                    connect.Dispose();
                }
                catch
                {
                    ret = -1;
                }
            }
            return ret;
        }

        //*******************************
        // Открыть базу
        //*******************************
        public int OpenDB()
        {
            int ret = 0;

            try
            {
                connect = new SQLiteConnection("Data Source=CursDB.db; Version=3");
                connect.Open();
                cmd = connect.CreateCommand();
            }
            catch
            {
                ret = -1;
            }
            return ret;
        }

        //*******************************
        // Добавить фигуру
        //*******************************
        public int AddFigur(Figur figur)
        {
            int ret = 0;

            cmd.CommandText = "INSERT INTO figur(name) "
                            + "VALUES ('" + figur.name + "')";
            try
            {
                cmd.ExecuteNonQuery();
                ret = (int)connect.LastInsertRowId;
            }
            catch
            {
                ret = -1;
            }
            return ret;
        }

        //*******************************
        // Изменит фигуру
        //*******************************
        public int WriteFigur(Figur figur)
        {
            int ret = 0;

            cmd.CommandText = "UPDATE figur SET name = '" + figur.name + "' WHERE cod = " + figur.cod.ToString();
            try
            {
                cmd.ExecuteNonQuery();
            }
            catch
            {
                ret = -1;
            }
            return ret;
        }

        //*******************************
        // Добавить вершины фигуру
        //*******************************
        public int AddVertexFigur(ArrayList vertexs)
        {
            int ret = 0;

            foreach (Vertex vertex in vertexs)
            {
                try
                {
                    cmd.CommandText = "INSERT INTO vertex(cod, num, x, y) VALUES ("
                                + vertex.cod + ","
                                + vertex.num + ","
                                + vertex.x + ","
                                + vertex.y + ")";
                    cmd.ExecuteNonQuery();
                }
                catch
                {
                    ret = -1;
                    break;
                }
            }
            return ret;
        }

        //*******************************
        // Удалить фигуру
        //*******************************
        public int DeleteFigur(Figur figur)
        {
            int ret = 0;

            //Поссмотрим ссылки
            if (ReadListProjectFigur(figur).Count == 0)
            {
                //Удалим вершины
                if (DeleteVertexFigur(figur) == 0)
                {
                    try
                    {
                        //Удалим фигуру
                        cmd.CommandText = "DELETE FROM figur WHERE cod = " + figur.cod.ToString();
                        cmd.ExecuteNonQuery();
                    }
                    catch
                    {
                        ret = -1;
                    }
                }
                else
                {
                    ret = -1;
                }
            }
            else
            {
                //Удалить нельзя, есть ссылка на этот документ
                ret = -1;
            }

            return ret;
        }

        //*******************************
        // Удалить вершины фигуру
        //*******************************
        public int DeleteVertexFigur(Figur figur)
        {
            int ret = 0;

            cmd.CommandText = "DELETE FROM vertex WHERE cod = " + figur.cod.ToString();
            try
            {
                cmd.ExecuteNonQuery();
            }
            catch
            {
                ret = -1;
            }
            return ret;
        }

        //*******************************
        // Получить список фигур
        //*******************************
        public ArrayList ReadListFigur()
        {
            ArrayList ret = new ArrayList();

            cmd.CommandText = "SELECT cod, name FROM figur ORDER BY name";
            try
            {
                SQLiteDataReader r = cmd.ExecuteReader();
                while (r.Read())
                {
                    ret.Add(new Figur((long)r["cod"], (string)r["name"]));
                }
                r.Close();
            }
            catch
            {
            }
            return ret;
        }

        //*******************************
        // Получить список вершин фигур
        //*******************************
        public ArrayList ReadListVertexFigur(Figur figur)
        {
            ArrayList ret = new ArrayList();

            cmd.CommandText = "SELECT cod, num, x, y FROM vertex WHERE cod = " + figur.cod.ToString() + " ORDER BY num";
            try
            {
                SQLiteDataReader r = cmd.ExecuteReader();
                while (r.Read())
                {
                    ret.Add(new Vertex((long)r["cod"], (long)r["num"], Convert.ToSingle(r["x"]), Convert.ToSingle(r["y"])));
                }
                r.Close();
            }
            catch
            {
            }
            return ret;
        }

        //*******************************
        // Список проектов по фигуре
        //*******************************
        public ArrayList ReadListProjectFigur(Figur figur)
        {
            ArrayList ret = new ArrayList();

            cmd.CommandText = "SELECT cod, codfigur, col FROM figurpr WHERE codfigur = " + figur.cod.ToString() + " ORDER BY codfigur";
            try
            {
                SQLiteDataReader r = cmd.ExecuteReader();
                while (r.Read())
                {
                    ret.Add(new FigurPr((long)r["cod"], (long)r["codfigur"], (long)r["col"]));
                }
                r.Close();
            }
            catch
            {
            }
            return ret;
        }

        //***********************************************************************************
        // ПРОЕКТ
        //***********************************************************************************

        //*******************************
        // Добавить проект
        //*******************************
        public int AddProgect(Project project)
        {
            int ret = 0;

            cmd.CommandText = "INSERT INTO project(name, listx, listy, step_p, step_a) VALUES ("
                            + "'" + project.name + "',"
                            + project.listx + ","
                            + project.listy + ","
                            + project.step_p + ","
                            + project.step_a + ")";
            try
            {
                cmd.ExecuteNonQuery();
                ret = (int)connect.LastInsertRowId;
            }
            catch
            {
                ret = -1;
            }
            return ret;
        }

        //*******************************
        // Добавить фигур проекта
        //*******************************
        public int AddFigurProject(ArrayList figurprs)
        {
            int ret = 0;

            foreach (FigurPr figurpr in figurprs)
            {
                try
                {
                    cmd.CommandText = "INSERT INTO figurpr(cod, codfigur, col) VALUES ("
                                + figurpr.cod + ","
                                + figurpr.codfigur + ","
                                + figurpr.col + ")";
                    cmd.ExecuteNonQuery();
                }
                catch
                {
                    ret = -1;
                    break;
                }
            }
            return ret;
        }

        //*******************************
        // Изменит фигуру
        //*******************************
        public int WriteProject(Project project)
        {
            int ret = 0;

            cmd.CommandText = "UPDATE project SET name = '" + project.name + "',"
                                               + "listx = " + project.listx + ","
                                               + "listy = " + project.listy + ","
                                               + "step_p = " + project.step_p + ","
                                               + "step_a = " + project.step_a + " "
                                               + " WHERE cod = " + project.cod.ToString();
            try
            {
                cmd.ExecuteNonQuery();
            }
            catch
            {
                ret = -1;
            }
            return ret;
        }

        //*******************************
        // Удалить проект
        //*******************************
        public int DeleteProject(Project project)
        {
            int ret = 0;

            //Удалим фигуры проекта
            if (DeleteFigurProject(project) == 0)
            {
                try
                {
                    //Удалим проект
                    cmd.CommandText = "DELETE FROM project WHERE cod = " + project.cod.ToString();
                    cmd.ExecuteNonQuery();
                }
                catch
                {
                    ret = -1;
                }
            }
            else
            {
                ret = -1;
            }

            return ret;
        }

        //*******************************
        // Удалить фигуры проекта
        //*******************************
        public int DeleteFigurProject(Project project)
        {
            int ret = 0;

            cmd.CommandText = "DELETE FROM figurpr WHERE cod = " + project.cod.ToString();
            try
            {
                cmd.ExecuteNonQuery();
            }
            catch
            {
                ret = -1;
            }
            return ret;
        }

        //*******************************
        // Получить список проектов
        //*******************************
        public ArrayList ReadListProject()
        {
            ArrayList ret = new ArrayList();

            cmd.CommandText = "SELECT cod, name, listx, listy, step_p, step_a FROM project ORDER BY name";
            try
            {
                SQLiteDataReader r = cmd.ExecuteReader();
                while (r.Read())
                {
                    ret.Add(new Project((long)r["cod"], (string)r["name"], (long)r["listx"], (long)r["listy"], (long)r["step_p"], (long)r["step_a"]));
                }
                r.Close();
            }
            catch
            {
            }
            return ret;
        }

        //**********************************
        // Получить список фигур проекта
        //**********************************
        public ArrayList ReadListFigurProject(Project project)
        {
            ArrayList ret = new ArrayList();

            cmd.CommandText = "SELECT cod, codfigur, col FROM figurpr WHERE cod = " + project.cod + " ORDER BY codfigur";
            try
            {
                SQLiteDataReader r = cmd.ExecuteReader();
                while (r.Read())
                {
                    ret.Add(new FigurPr((long)r["cod"], (long)r["codfigur"], (long)r["col"]));
                }
                r.Close();
            }
            catch
            {
            }
            return ret;
        }

    }
}