using System.Text;
using NStack;
using Terminal.Gui;
using Color = Terminal.Gui.Color;
using Attribute = Terminal.Gui.Attribute;
using SteamPrefill.CliCommands.SelectAppsBeta;

namespace SteamPrefill.CliCommands
{
    //TODO Enter to finish selection
    //TODO Finish search implementation
    //TODO implement sorting by recently played games
    //TODO include more metadata in the list view, like year/minutes played/last played/etc
    //TODO selected rows need some more coloring to differentiate what is selected
    //TODO Can I format the list items to show checked with [X] like the original
    //TODO can the selected items check show as blue
    //TODO get rid of ugly green outline
    //TODO Search Box - control+a in text box needs to select all text
    //TODO Search Box - Need a way to easily clear current query
    //TODO update readme with new pictures
    //TODO it looks like the year being displayed is not the actual original release year, but rather when the game was added to steam.  Look at the EA games
    public partial class SelectAppsTui
    {
        private ListView _listView;
        private TextField _searchBox;
        private StatusBar statusBar;

        private AppInfoDataSource ListViewDataSource => (AppInfoDataSource)_listView.Source;

        readonly ColorScheme _elementColorScheme = new ColorScheme
        {
            Normal = new Attribute(foreground: Color.White, background: Color.Black),
            HotNormal = new Attribute(foreground: Color.White, background: Color.Black),
            Focus = new Attribute(foreground: Color.BrightBlue, background: Color.Black),
            HotFocus = new Attribute(foreground: Color.BrightBlue, background: Color.Black),
        };

        public SelectAppsTui(List<AppInfo> appInfos)
        {
            //TODO what is the correct encoding to use
            Console.OutputEncoding = Encoding.Default;
            //TODO determine the correct console
            Application.UseSystemConsole = false;

            InitLayout(appInfos);

            // Configuring status bar actions
            statusBar.Items = new StatusItem[] {
                new StatusItem(Key.Esc, "~ESC~ to Quit", () =>
                {
                    Application.RequestStop(Application.Top);
                    Application.Top.SetNeedsDisplay();
                }),
                new StatusItem (Key.CharMask, "~↑/↓/PgUp/PgDn~ to navigate", null),
                new StatusItem (Key.CharMask, "~Space~ to select", null),
                new StatusItem (Key.CtrlMask | Key.A, "~CTRL-A~ Select All", () =>
                {
                    ListViewDataSource.SetAllSelected(true);
                    _listView.SetNeedsDisplay();
                }),
                new StatusItem (Key.CtrlMask | Key.C, "~CTRL-C~ Clear All", () =>
                {
                    ListViewDataSource.SetAllSelected(false);
                    _listView.SetNeedsDisplay();
                }),
                new StatusItem (Key.Enter, "~Enter~ to Save", () =>
                {
                    //TODO implement save
                }),
            };
        }

        public void Run()
        {
            _searchBox.SetFocus();
            Application.Run(Application.Top);
            Application.Shutdown();
        }

        private void SearchBox_OnTextChanged(ustring obj)
        {
            var searchText = _searchBox.Text.ToString();

            ListViewDataSource.FilterItems(searchText);
            _listView.SetNeedsDisplay();
        }

        private void SortNameButton_OnClicked()
        {
            ListViewDataSource.SortName();
            _listView.SetNeedsDisplay();
        }

        private void SortYearButton_OnClicked()
        {
            ListViewDataSource.SortYear();
            _listView.SetNeedsDisplay();
        }

        private void ListView_RowRender(ListViewRowEventArgs obj)
        {
            //TODO implement
            //if (obj.Row == _listView.SelectedItem)
            //{
            //    return;
            //}
            //if (_listView.AllowsMarking && _listView.Source.IsMarked(obj.Row))
            //{
            //    //TODO use this to color the rows or something
            //    //obj.RowAttribute = new Attribute(Color.BrightRed, Color.BrightYellow);
            //    return;
            //}
        }
    }
}
