using System;
using System.Globalization;
using System.Windows.Forms;

namespace PaperDays
{
    public static class CourseDates
    {
        /// <summary>
        /// 处理CourseDates中的 First 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public static DateTime First(CourseRow c, DateTime start)
        {
            return c.FirstDate.HasValue ? c.FirstDate.Value.Date : start.Date.AddDays((c.Day - ((int)start.DayOfWeek + 6) % 7 - 1 + 7) % 7);
        }

        /// <summary>
        /// 处理CourseDates中的 Optional 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public static DateTime? Optional(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return null;
            DateTime date;
            if (!DateTime.TryParseExact(value, new[] { "yyyy-MM-dd", "yyyy-M-d", "yyyy/MM/dd", "yyyy/M/d" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                throw new Exception("日期请填写 yyyy-MM-dd，或双击选择；也可留空自动安排。");
            return date;
        }
    }

    public partial class CourseImportDialog
    {
        /// <summary>
        /// 选择CourseDates中的 ChooseCourseValue 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        async void ChooseCourseValue(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != 7)
                return;
            grid.EndEdit();
            var row = grid.Rows[e.RowIndex];
            int col = e.ColumnIndex;
            using (var form = new Form())
            {
                UI.Style(form, col == 7 ? "首课日期" : "课程时间", 350, 190);
                var picker = new DateField
                {
                    Left = 24,
                    Top = 24,
                    Width = 220
                };
                try
                {
                    picker.Value = col == 7 ? (CourseDates.Optional(Convert.ToString(row.Cells[col].Value)) ?? DateTime.Today) : DateTime.Today + CourseImporter.Time(Convert.ToString(row.Cells[col].Value));
                }
                catch
                {
                    picker.Value = DateTime.Today.AddHours(8);
                }

                form.Controls.Add(picker);
                UI.Button(form, "应用", 204, 90, 114, delegate
                {
                    if (!picker.CommitInput())
                    {
                        Notice.Show(form, "请输入有效日期，例如 2026-11-04。");
                        return;
                    }

                    row.Cells[col].Value = picker.Value.ToString(col == 7 ? "yyyy-MM-dd" : "HH:mm");
                    if (col == 7)
                    {
                        choosing = true;
                        row.Cells[0].Value = "周" + "一二三四五六日"[((int)picker.Value.DayOfWeek + 6) % 7];
                        choosing = false;
                    }

                    form.Close();
                    UpdateStatus();
                });
                UI.Button(form, col == 7 ? "自动安排" : "取消", 24, 90, 130, delegate
                {
                    if (col == 7)
                    {
                        row.Cells[col].Value = "";
                        UpdateStatus();
                    }

                    form.Close();
                });
                await IndependentWindows.Show(form);
            }
        }
    }
}