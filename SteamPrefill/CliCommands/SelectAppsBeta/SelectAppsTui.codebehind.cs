using Terminal.Gui;
using Terminal.Gui.Graphs;
using Color = Terminal.Gui.Color;
using Attribute = Terminal.Gui.Attribute;

namespace SteamPrefill.CliCommands.SelectAppsBeta
{
    public partial class SelectAppsTui
    {
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
            Application.Top.Add(window);

            #region First Row - Sorting

            var sortLabel = new Label("Sort:")
            {
                X = 1,
                ColorScheme = _buttonColorScheme
            };
            var sortNameButton = new Button("Name")
            {
                X = Pos.Right(sortLabel) + 1,
                ColorScheme = _buttonColorScheme
            };
            sortNameButton.Clicked += SortName_OnClicked;

            var sortYearButton = new Button("Year")
            {
                X = Pos.Right(sortNameButton) + 1,
                ColorScheme = _buttonColorScheme
            };
            sortYearButton.Clicked += SortYear_OnClicked;

            var sortPlaytimeButton = new Button("Recent Playtime")
            {
                X = Pos.Right(sortYearButton) + 1,
                ColorScheme = _buttonColorScheme
            };
            sortPlaytimeButton.Clicked += SortPlaytime_OnClicked;

            var sortSelectedButton = new Button("Selected")
            {
                X = Pos.Right(sortPlaytimeButton) + 1,
                ColorScheme = _buttonColorScheme
            };
            sortSelectedButton.Clicked += SortSelected_OnClicked;

            window.Add(sortLabel, sortNameButton, sortYearButton, sortPlaytimeButton, sortSelectedButton);

            #endregion

            #region Second Row

            var searchLabel = new Label("Search: ")
            {
                X = 1,
                Y = 2
            };
            _searchBox = new TextField
            {
                X = Pos.Right(searchLabel) + 1,
                Y = 2,
                Width = 50
            };
            _searchBox.TextChanged += SearchBox_OnTextChanged;
            _searchBox.AddKeyBinding(Key.CtrlMask | Key.A, Command.SelectAll);
            window.Add(searchLabel, _searchBox);

            #endregion

            var lineView = new LineView(Orientation.Horizontal)
            {
                Y = 3,
                Width = Dim.Fill()
            };
            window.Add(lineView);

            headerLabel = new Label
            {
                Y = 4,
                Width = Dim.Fill()
            };
            window.Add(headerLabel);

            var lineView2 = new LineView(Orientation.Horizontal)
            {
                Y = 5,
                Width = Dim.Fill()
            };
            window.Add(lineView2);

            _listView = new ListView
            {
                X = 1,
                Y = 6,
                Height = Dim.Fill(),
                Width = Dim.Fill(1),
                ColorScheme = new ColorScheme
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
            _statusBar = new StatusBar
            {
                Visible = true,
                ColorScheme = new ColorScheme
                {
                    Normal = new Attribute(Color.White, Color.Black),
                    HotNormal = new Attribute(foreground: Color.BrightGreen, background: Color.Black),
                }
            };
            Application.Top.Add(_statusBar);
        }
    }
}
