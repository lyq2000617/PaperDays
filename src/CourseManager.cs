using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace PaperDays
{
    public partial class CourseImportDialog
    {
        /// <summary>
        /// 载入CourseManager中的 LoadPeriods 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        internal void LoadPeriods(List<CourseRow> courses)
        {
            periodGrid.Rows.Clear();
            var map = SmartCourses.ReadPeriods(sheets ?? new List<SheetData>());
            foreach (string key in courses.SelectMany(c => new[] { c.Period, c.EndPeriod }).Where(k => !String.IsNullOrWhiteSpace(k)).Distinct())
            {
                TimeSpan[] times;
                map.TryGetValue(key, out times);
                periodGrid.Rows.Add(key, times == null ? "" : times[0].ToString(@"hh\:mm"), times == null ? "" : times[1].ToString(@"hh\:mm"));
            }

            ApplyKnownPeriods(map, false);
        }

        /// <summary>
        /// 将兼容节次映射应用到预览课程，不单独创建日程。
        /// </summary>
        internal void ApplyPeriods()
        {
            periodGrid.EndEdit();
            if (periodGrid.Rows.Count == 0)
                throw new Exception("先选择含节次的课表，或添加需要设置的节次。");
            var map = new Dictionary<string, TimeSpan[]>();
            foreach (DataGridViewRow row in periodGrid.Rows)
            {
                string key = SmartCourses.Period(Convert.ToString(row.Cells[0].Value));
                if (key == null)
                    throw new Exception("节次请填写 1、2 或第一节等编号。");
                if (map.ContainsKey(key))
                    throw new Exception("第 " + key + " 节重复，请合并成一行。");
                try
                {
                    var a = CourseImporter.Time(Convert.ToString(row.Cells[1].Value));
                    var b = CourseImporter.Time(Convert.ToString(row.Cells[2].Value));
                    if (b <= a)
                        throw new Exception("结束时间须晚于开始时间");
                    map[key] = new[]
                    {
                        a,
                        b
                    };
                }
                catch (Exception ex)
                {
                    throw new Exception("第 " + key + " 节：" + ex.Message);
                }
            }

            ApplyKnownPeriods(map, true);
            UpdateStatus();
        }

        /// <summary>
        /// 应用CourseManager中的 ApplyKnownPeriods 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        void ApplyKnownPeriods(Dictionary<string, TimeSpan[]> map, bool overwrite)
        {
            foreach (DataGridViewRow row in grid.Rows)
            {
                var c = row.Tag as CourseRow;
                if (c == null)
                    continue;
                TimeSpan[] times;
                if (c.Period != null && map.TryGetValue(c.Period, out times) && (overwrite || String.IsNullOrWhiteSpace(Convert.ToString(row.Cells[1].Value))))
                    row.Cells[1].Value = times[0].ToString(@"hh\:mm");
                if (c.EndPeriod != null && map.TryGetValue(c.EndPeriod, out times) && (overwrite || String.IsNullOrWhiteSpace(Convert.ToString(row.Cells[2].Value))))
                    row.Cells[2].Value = times[1].ToString(@"hh\:mm");
            }
        }

        internal DataGridView PeriodGrid
        {
            /// <summary>
            /// 读取PeriodGrid属性，供CourseManager使用。
            /// </summary>
            get
            {
                return periodGrid;
            }
        }
    }
}