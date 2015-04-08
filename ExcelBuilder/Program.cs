using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Office.Interop.Excel;
using System.Reflection;
using System.Data;

namespace ExcelBuilder
{
    public class Builder
    {
        private Application excel { get; set; }

        public Builder()
        {
            excel = new Application();
            excel.Visible = true;
            var workbook = excel.Workbooks.Add();
        }

        public void AddSheet(List<List<dynamic>> values)
        {
            var sheet = excel.ActiveSheet;
            sheet.Columns.ColumnWidth = 20;
            for (int i = 0; i < values.Count; i++)
            {
                for (int j = 0; j < values[i].Count; j++)
                {
                    try
                    {
                        sheet.Cells[i + 1, j + 1] = values[i][j];
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }
        }

        public void Save(string name)
        {
            excel.ActiveWorkbook.SaveAs("/GEW/Excels/" + name + ".xlsx");
        }

        public static void Main()
        {

        }
    }
}
