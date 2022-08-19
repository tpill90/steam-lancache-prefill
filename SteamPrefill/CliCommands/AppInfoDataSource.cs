using System.Collections;
using NStack;
using Terminal.Gui;

namespace SteamPrefill.CliCommands
{
    internal class AppInfoDataSource : IListDataSource
    {
        public int Count => _currAppInfo != null ? _currAppInfo.Count : 0;

        private int _maxItemLength;
        public int Length => _maxItemLength;

        private List<AppInfo> _originalAppInfo;
        private List<AppInfo> _currAppInfo;

        public AppInfoDataSource(List<AppInfo> itemList)
        {
            _currAppInfo = itemList;
            _originalAppInfo = itemList.ToList();
            _maxItemLength = _currAppInfo.Select(e => FormatItemString(e).Length).Max();
        }

        public bool IsMarked(int item)
        {
            if (item >= 0 && item < _currAppInfo.Count)
            {
                return _currAppInfo[item].IsSelected;
            }

            return false;
        }

        public void SetMark(int item, bool value)
        {
            if (item >= 0 && item < _currAppInfo.Count)
            {
                _currAppInfo[item].IsSelected = value;
            }
        }

        //TODO comment
        public void SetAllSelected(bool isSelected)
        {
            for (int i = 0; i < _currAppInfo.Count; i++)
            {
                _currAppInfo[i].IsSelected = isSelected;
            }
        }

        public void FilterItems(string searchText)
        {
            _currAppInfo = _originalAppInfo.Where(e => e.Name.Contains(searchText, StringComparison.InvariantCultureIgnoreCase)).ToList();
        }

        public void SortName()
        {
            _currAppInfo = _currAppInfo.OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private bool _sortYearToggle;
        public void SortYear()
        {
            if (_sortYearToggle)
            {
                _currAppInfo = _currAppInfo.OrderBy(e => e.SteamReleaseDate).ToList();
            }
            else
            {
                _currAppInfo = _currAppInfo.OrderByDescending(e => e.SteamReleaseDate).ToList();
            }
            _sortYearToggle = !_sortYearToggle;
        }


        //TODO better name
        private string FormatItemString(AppInfo item)
        {
            var nameColumn = String.Format($"{{0,{-45}}}", item.Name);
            var formattedReleaseDate = String.Format($"{{0,{-20}}}", item.SteamReleaseDate?.Date.ToString("yyyy"));
            var formattedType = item.Type.Name;

            return $"{nameColumn} {formattedReleaseDate} {formattedType}";
        }

        public void Render(ListView container, ConsoleDriver driver, bool selected, int item, int col, int line, int width, int start = 0)
        {
            container.Move(col, line);
            RenderUstr(driver, FormatItemString(_currAppInfo[item]), col, line, width, start);
        }

        // A slightly adapted method from: https://github.com/gui-cs/Terminal.Gui/blob/fc1faba7452ccbdf49028ac49f0c9f0f42bbae91/Terminal.Gui/Views/ListView.cs#L433-L461
        //TODO not entirely sure why this is here.  Could possibly be an extension method?
        private void RenderUstr(ConsoleDriver driver, ustring ustr, int col, int line, int width, int start = 0)
        {
            int used = 0;
            int index = start;
            while (index < ustr.Length)
            {
                (var rune, var size) = Utf8.DecodeRune(ustr, index, index - ustr.Length);
                var count = Rune.ColumnWidth(rune);
                if (used + count >= width) break;
                driver.AddRune(rune);
                used += count;
                index += size;
            }

            while (used < width)
            {
                driver.AddRune(' ');
                used++;
            }
        }

        public IList ToList()
        {
            return _currAppInfo;
        }
    }
}