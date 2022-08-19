using SteamPrefill.CliCommands.SelectAppsBeta;
using Terminal.Gui;
using Color = Terminal.Gui.Color;
using Attribute = Terminal.Gui.Attribute;

namespace SteamPrefill.CliCommands
{
    public partial class SelectAppsTui
    {
        //TODO determine if this dispose is required
        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "<Pending>")]
        private void InitLayout(List<AppInfo> appInfos)
        {
            var window = new Window("")
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                ColorScheme = Colors.TopLevel
            };
            var top1 = Application.Top;
            top1.Add(window);

            #region First Row

            //TODO Do I really need to pass the color scheme to each element?
            var sortLabel = new Label("Sort:")
            {
                X = 1,
                ColorScheme = _elementColorScheme
            };
            var sortNameButton = new Button("Name")
            {
                X = Pos.Right(sortLabel) + 1,
                ColorScheme = _elementColorScheme
            };
            sortNameButton.Clicked += SortNameButton_OnClicked;

            var sortYearButton = new Button("Year")
            {
                X = Pos.Right(sortNameButton) + 1,
                ColorScheme = _elementColorScheme
            };
            sortYearButton.Clicked += SortYearButton_OnClicked;

            window.Add(sortLabel, sortNameButton, sortYearButton);

            #endregion

            var searchLabel = new Label("Search: ")
            {
                X = 1,
                Y = 3
            };
            window.Add(searchLabel);

            _searchBox = new TextField()
            {
                X = Pos.Right(searchLabel) + 1,
                Y = 3,
                Width = 50
            };
            _searchBox.TextChanged += SearchBox_OnTextChanged;
            window.Add(_searchBox);

            _listView = new ListView
            {
                X = 1,
                Y = 5,
                Height = Dim.Fill(),
                Width = Dim.Fill(1),
                ColorScheme = new ColorScheme()
                {
                    Normal = new Attribute(foreground: Color.Gray, background: Color.Black),
                    HotNormal = new Attribute(foreground: Color.Gray, background: Color.Black),
                    Focus = new Attribute(foreground: Color.Cyan, background: Color.Black),
                },
                AllowsMarking = true,
                AllowsMultipleSelection = true,

            };
            _listView.RowRender += ListView_RowRender;
            _listView.Source = new AppInfoDataSource(appInfos);
            window.Add(_listView);

            //TODO should the different actions have different colors?
            var statusBar = new StatusBar
            {
                Visible = true,
                ColorScheme = new ColorScheme
                {
                    Normal = new Attribute(Color.White, Color.Black),
                    HotNormal = new Attribute(foreground: Color.BrightGreen, background: Color.Black),
                }
            };

            //TODO I don't like that these are hidden
            statusBar.Items = new StatusItem[] {
                new StatusItem(Key.Esc, "~ESC~ to Quit", () =>
                {
                    Application.RequestStop(top1);
                    top1.SetNeedsDisplay();
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
            top1.Add(statusBar);
        }
    }
}
