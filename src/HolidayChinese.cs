using System;
using System.Collections.Generic;

namespace PaperDays
{
    public static class HolidayChinese
    {
        static Dictionary<string, string> names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        /// <summary>
        /// 初始化HolidayChinese的HolidayChinese，准备状态、布局与必要的事件绑定。
        /// </summary>
        static HolidayChinese()
        {
            string pairs = @"New Year's Day|元旦
75th anniversary of the uprising of June 17, 1953|1953年六月十七日起义75周年纪念日
Australia Day|澳大利亚日
Labour Day|劳动节
Canberra Day|堪培拉日
Adelaide Cup Day|阿德莱德杯日
Eight Hours Day|八小时工作制纪念日
Good Friday|耶稣受难日
Holy Saturday|复活节前夕
Easter Sunday|复活节
Easter Monday|复活节星期一
Anzac Day|澳新军团日
May Day|五月劳动节
Reconciliation Day|和解日
Western Australia Day|西澳大利亚日
King's Birthday|国王诞辰
Picnic Day|野餐日
Friday before AFL Grand Final|澳式足球决赛前假日
Melbourne Cup|墨尔本杯日
Christmas Day|圣诞节
St. Stephen's Day|圣斯德望日
Louis Riel Day|路易·里尔日
Islander Day|岛民日
Heritage Day|文化遗产日
Family Day|家庭日
Saint Patrick's Day|圣帕特里克节
Saint George's Day|圣乔治节
National Patriots' Day|爱国者日
Victoria Day|维多利亚日
National Aboriginal Day|原住民日
Discovery Day|发现日
National Holiday|国庆假日
Canada Day|加拿大国庆日
Orangemen's Day|奥兰治日
Civic Holiday|市民假日
British Columbia Day|不列颠哥伦比亚日
New Brunswick Day|新不伦瑞克日
Natal Day|建省纪念日
Saskatchewan Day|萨斯喀彻温日
Gold Cup Parade Day|金杯游行日
National Day for Truth and Reconciliation|真相与和解日
Thanksgiving|感恩节
Armistice Day|停战纪念日
Remembrance Day|战争纪念日
Epiphany|主显节
International Women's Day|国际妇女节
Ascension Day|耶稣升天节
Pentecost|圣灵降临节
Whit Monday|圣灵降临节星期一
Corpus Christi|基督圣体节
Assumption Day|圣母升天节
World Children's Day|世界儿童日
German Unity Day|德国统一日
Reformation Day|宗教改革纪念日
All Saints' Day|诸圣节
Repentance and Prayer Day|忏悔与祈祷日
Victory in Europe Day|欧洲胜利日
Bastille Day|法国国庆日
2 January|元旦次日假日
Early May Bank Holiday|五月初银行假日
Spring Bank Holiday|春季银行假日
World Cup Bank Holiday|世界杯银行假日
Battle of the Boyne|博因河战役纪念日
Summer Bank Holiday|夏季银行假日
Saint Andrew's Day|圣安德鲁日
Coming of Age Day|成人节
Foundation Day|建国纪念日
The Emperor's Birthday|天皇诞辰
Vernal Equinox Day|春分日
Shōwa Day|昭和日
Constitution Memorial Day|宪法纪念日
Greenery Day|绿之日
Children's Day|儿童节
Marine Day|海之日
Mountain Day|山之日
Respect for the Aged Day|敬老日
Autumnal Equinox Day|秋分日
Sports Day|体育日
Culture Day|文化日
Labour Thanksgiving Day|勤劳感谢日
Lunar New Year|农历新年
Independence Movement Day|三一节
Buddha's Birthday|佛诞日
Local Election Day|地方选举日
Memorial Day|阵亡者纪念日
Constitution Day|宪法日
Liberation Day|光复节
Chuseok|秋夕
National Foundation Day|开天节
Hangul Day|韩文日
Day after New Year's Day|元旦次日假日
Waitangi Day|怀唐伊日
Matariki|毛利新年
Boxing Day|节礼日
Martin Luther King, Jr. Day|马丁·路德·金纪念日
Lincoln's Birthday|林肯诞辰
Presidents Day|总统日
Truman Day|杜鲁门日
Juneteenth National Independence Day|六月节
Independence Day|独立日
Columbus Day|哥伦布日
Indigenous Peoples' Day|原住民日
Veterans Day|退伍军人节
Thanksgiving Day|感恩节";
            foreach (var line in pairs.Split('\n'))
            {
                var p = line.Trim().Split('|');
                names[p[0]] = p[1];
            }

            string[] regions =
            {
                "Wellington|惠灵顿",
                "Auckland|奥克兰",
                "Nelson|尼尔森",
                "Taranaki|塔拉纳基",
                "Otago|奥塔哥",
                "Southland|南地",
                "Canterbury (South)|南坎特伯雷",
                "Hawke's Bay|霍克斯湾",
                "Marlborough|马尔堡",
                "Canterbury|坎特伯雷",
                "Chatham Islands|查塔姆群岛",
                "Westland|西地"
            };
            foreach (var region in regions)
            {
                var p = region.Split('|');
                names[p[0] + " Anniversary Day"] = p[1] + "周年纪念日";
            }
        }

        /// <summary>
        /// 处理HolidayChinese中的 Name 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public static string Name(string text, string code)
        {
            string value;
            foreach (string suffix in new[]
            {
                " (observed)",
                " (Observed)",
                " (substitute day)",
                " (Substitute day)"
            }

            )
                if ((text ?? "").EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    return Name(text.Substring(0, text.Length - suffix.Length), code) + "（补休）";
            string key = (text ?? "").Replace(" (Tentative Date)", "");
            if (names.TryGetValue(key, out value))
            {
                if (key == "Memorial Day" && code == "KR")
                    value = "显忠日";
                if (key == "St. Stephen's Day" && (code == "AU" || code == "GB"))
                    value = "节礼日";
                return value + (key == text ? "" : "（暂定）");
            }

            return "公共假日（译名待补）";
        }

        /// <summary>
        /// 处理HolidayChinese中的 Country 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public static string Country(string code)
        {
            int i = Array.IndexOf(Calendars.Codes, code);
            return i >= 0 ? Calendars.Names[i] : code;
        }
    }
}