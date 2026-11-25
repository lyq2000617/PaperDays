using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PaperDays
{
    public static class LegacyExcel
    {
        /// <summary>
        /// 读取并解析本地或系统数据，调用方负责处理失败。
        /// </summary>
        public static List<SheetData> Read(string path)
        {
            dynamic app = null, book = null, books = null;
            try
            {
                var type = Type.GetTypeFromProgID("Excel.Application");
                if (type == null)
                    throw new Exception("读取 .xls 需要安装 Microsoft Excel；也可用原软件另存为 .xlsx，表格布局不必修改。");
                app = Activator.CreateInstance(type);
                app.Visible = false;
                app.DisplayAlerts = false;
                app.AutomationSecurity = 3;
                app.AskToUpdateLinks = false;
                books = app.Workbooks;
                book = books.Open(Filename: path, UpdateLinks: 0, ReadOnly: true, Password: "", WriteResPassword: "", IgnoreReadOnlyRecommended: true, Notify: false, AddToMru: false);
                var result = new List<SheetData>();
                for (int i = 1; i <= book.Worksheets.Count; i++)
                {
                    dynamic sheet = book.Worksheets[i], range = null;
                    try
                    {
                        range = sheet.UsedRange;
                        if (range.Rows.Count > 1000 || range.Columns.Count > 50)
                            throw new Exception("旧版 Excel 课表过大，请另存为 .xlsx");
                        var data = new SheetData
                        {
                            Name = (string)sheet.Name
                        };
                        int offsetRow = range.Row - 1, offsetCol = range.Column - 1;
                        for (int r = 0; r < offsetRow + range.Rows.Count; r++)
                            data.Rows.Add(new List<string>());
                        for (int r = 1; r <= range.Rows.Count; r++)
                        {
                            var row = data.Rows[offsetRow + r - 1];
                            for (int x = 0; x < offsetCol; x++)
                                row.Add("");
                            for (int c = 1; c <= range.Columns.Count; c++)
                            {
                                dynamic cell = range.Cells[r, c], area = null;
                                try
                                {
                                    row.Add(Convert.ToString(cell.Value2));
                                    if ((bool)cell.MergeCells)
                                    {
                                        area = cell.MergeArea;
                                        string address = Convert.ToString(area.Address).Replace("$", "");
                                        if (!data.Merges.Contains(address))
                                            data.Merges.Add(address);
                                    }
                                }
                                finally
                                {
                                    Release(area);
                                    Release(cell);
                                }
                            }
                        }

                        result.Add(data);
                    }
                    finally
                    {
                        Release(range);
                        Release(sheet);
                    }
                }

                return result;
            }
            catch (COMException ex)
            {
                throw new Exception("Excel 无法读取该文件：" + ex.Message);
            }
            finally
            {
                try
                {
                    if (book != null)
                        book.Close(false);
                }
                catch
                {
                }

                try
                {
                    if (app != null)
                        app.Quit();
                }
                catch
                {
                }

                Release(book);
                Release(books);
                Release(app);
            }
        }

        /// <summary>
        /// 释放系统或 COM 资源，避免句柄泄漏。
        /// </summary>
        static void Release(object value)
        {
            if (value != null && Marshal.IsComObject(value))
                Marshal.ReleaseComObject(value);
        }
    }
}