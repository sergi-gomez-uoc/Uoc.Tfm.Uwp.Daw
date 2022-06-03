using System;
using System.Collections.Generic;
using System.Linq;
using Uoc.Tfm.Uwp.Daw.Extensions;
using Uoc.Tfm.Uwp.Daw.Interfaces;
using Uoc.Tfm.Uwp.Daw.Model;
using Windows.UI;
using Windows.UI.Input;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Windows.Devices.Input;

namespace Uoc.Tfm.Uwp.Daw.Controls
{
    public sealed partial class PianoRollUserControl : UserControl, ControlInterface
    {
        private Guid _trackId;

        private readonly List<Score> _scores;

        public event EventHandler ScoreChanged;

        readonly int cellWidth = 40;
        readonly int cellHeight = 15;
        readonly int columns = 80;
        readonly int rows = 88;
        readonly int firstColumnWidth = 40;


        public PianoRollUserControl(Guid trackId)
        {
            this.InitializeComponent();
            _trackId = trackId;

            //var song = (App.Current as App)._song;
            var track = this.GetTrack(_trackId); // song.Tracks.FirstOrDefault(x => x.TrackId == trackId);

            if (track.Scores == null)
            {
                track.Scores = new List<Score>();
                _scores = track.Scores.ToList();
                DrawScore();
            }
            else
            {
                _scores = track.Scores.ToList();
                ClearScores();
                DrawScore();
                LoadScore();
            }
        }

        private void Canvas_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var currentPoint = e.GetCurrentPoint(Canvas);

            if (currentPoint.Position.X < firstColumnWidth)
            {
                return;
            }

            if (e.Pointer.PointerDeviceType == PointerDeviceType.Mouse)
            {
                var properties = e.GetCurrentPoint(Canvas).Properties;
                if (properties == null)
                {
                    return;
                }

                if (properties.IsLeftButtonPressed)
                {
                    isMousePressed = true;
                    initPoint = currentPoint;

                    tempScore = GetScore(currentPoint);
                    //tempScore = score;
                    tempRectangle = DrawRectangle(tempScore);
                }

                /*
                if (properties.IsRightButtonPressed)
                {
                    var score = GetScore(e.GetCurrentPoint(this));
                    var sty = _scores.FirstOrDefault(x => x.Row == score.Row && x.Column >= score.Column && x.Column + x.Length >= score.Column);

                    var tt = this.GetTrack(TrackId).Scores.FirstOrDefault(x => x.Row == score.Row && score.Column >= x.Column && score.Column <= (x.Column + x.Length));
                    if (sty != null)
                    {
                        _scores.Remove(sty);
                        (App.Current as App)._song.Tracks.FirstOrDefault(x => x.TrackId == TrackId).Scores.Remove(sty);
                        ScoreChanged?.Invoke(this, EventArgs.Empty);
                    }

                    Canvas.Children.Remove(re);
                    re = null;
                }*/

            }
        }

        private void Rect_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType != PointerDeviceType.Mouse)
            {
                return;
            }

            var properties = e.GetCurrentPoint(this).Properties;
            if (properties == null)
            {
                return;
            }

            if (properties.IsLeftButtonPressed)
            {
                e.Handled = true;
            }
            else
            {
                var rectangle = (e.OriginalSource as Rectangle);

                if (rectangle == null)
                {
                    return;
                }

                (rectangle.Parent as Canvas).Children.Remove(rectangle);

                //var positionX = e.GetCurrentPoint(Canvas).Position.X;

                var score = GetScore(e.GetCurrentPoint(Canvas));
                var track = this.GetTrack(_trackId);

                var trackScore = track
                                    .Scores
                                    .FirstOrDefault(x =>
                                            score.Row == x.Row &&
                                            score.Column >= x.Column &&
                                            score.Column <= (x.Column + x.Length));

                if (trackScore != null)
                {
                    track.Scores.Remove(trackScore);
                }

            }
        }

        private Score GetScore(PointerPoint point, int length = 1)
        {
            var x = point.Position.X - firstColumnWidth;
            var y = point.Position.Y;

            var posX = (int)x / cellWidth;
            var posY = (int)y / cellHeight;

            return new Score
            {
                Row = posY,
                Column = posX,
                Length = length
            };
        }

        private void DrawScore()
        {
            DrawRectangles();
            DrawLines();
        }

        private void DrawRectangles()
        {
            for (int i = 0; i < rows; i++)
            {
                var rect = new Rectangle();
                rect.Width = cellWidth * columns;
                rect.Height = cellHeight;
                rect.Fill = IsBlackKey(i) ?
                                new SolidColorBrush(Color.FromArgb(120, 120, 120, 128)) :
                                new SolidColorBrush(Color.FromArgb(10, 10, 10, 128));
                rect.Opacity = 1;

                Canvas.SetLeft(rect, firstColumnWidth);
                Canvas.SetTop(rect, cellHeight * i);
                Canvas.SetZIndex(rect, 10);

                Canvas.Children.Add(rect);


                var firstColumnRectangle = new Rectangle();

                firstColumnRectangle.Width = firstColumnWidth;
                firstColumnRectangle.Height = cellHeight;
                firstColumnRectangle.Fill = new SolidColorBrush(GetColor(i));

                Canvas.SetLeft(firstColumnRectangle, 0);
                Canvas.SetTop(firstColumnRectangle, cellHeight * i);
                Canvas.SetZIndex(firstColumnRectangle, 10);

                Canvas.Children.Add(firstColumnRectangle);

                if (i > 0)
                {
                    var ln = new Line();

                    ln.X1 = firstColumnWidth;
                    ln.X2 = cellWidth * columns;
                    ln.Y1 = cellHeight * i;
                    ln.Y2 = cellHeight * i;
                    ln.StrokeThickness = 1;
                    ln.Stroke = new SolidColorBrush(Colors.DarkGray);
                    ln.Opacity = 0.2;
                    Canvas.Children.Add(ln);
                    Canvas.SetZIndex(ln, 10);
                }
            }
        }

        private Color GetColor(int row)
        {
            //var blackKeys = new int[] { 2, 4, 6, 9, 11 };

            //if (blackKeys.Contains((row) % 12))
            //{
            //    return Colors.Black;
            //}

            //return Colors.White;

            return IsBlackKey(row) ? Colors.Black : Colors.White;
        }

        private bool IsBlackKey(int row)
        {
            var keysinOctave = 12;

            var blackKeys = new int[] { 2, 4, 6, 9, 11 };

            if (blackKeys.Contains((row) % keysinOctave))
            {
                return true;
            }
            return false;
        }

        private string GetKeyText(int row)
        {

            var blackKeys = new int[] { 2, 4, 6, 9, 11 };
            var keys = new string[] { "C", "B", "A", "G", "F", "E", "D" };

            return null;
        }

        private void DrawLines()
        {
            for (int i = 0; i < columns; i++)
            {
                var line = new Line();


                line.X1 = cellWidth * i + firstColumnWidth;
                line.X2 = cellWidth * i + firstColumnWidth;
                line.Y1 = 0;
                line.Y2 = cellHeight * rows;
                line.StrokeThickness = i % 16 == 0 ? 3 : i % 4 == 0 ? 2 : 1;
                line.Stroke = i % 2 == 0 ? new SolidColorBrush(Colors.DarkGray) : new SolidColorBrush(Colors.Gray);
                line.Opacity = 0.2;
                Canvas.Children.Add(line);
            }


            //var kk = new Line();
            //kk.X1 = 20;
            //kk.X2 = 20;
            //kk.Y1 = 8;
            //kk.Y2 = cellHeight * rows;
            //kk.Stroke = new SolidColorBrush(Windows.UI.Colors.Black);
            //kk.StrokeThickness = 2;
            //Canvas.Children.Add(kk);


            //var tri = new Polygon();
            //tri.Points.Add(new Point(10, 0));
            //tri.Points.Add(new Point(20, 8));
            //tri.Points.Add(new Point(30, 0));
            //tri.Fill = new SolidColorBrush(Windows.UI.Colors.Red);
            //tri.Stroke = new SolidColorBrush(Windows.UI.Colors.White);
            //tri.StrokeThickness = 3;
            //Canvas.Children.Add(tri);
            //tri.PointerPressed += Tri_PointerPressed;
            //tri.PointerMoved += Tri_PointerMoved;
            //tri.PointerReleased += Tri_PointerReleased;
            //tri.PointerExited += Tri_PointerExited;


            //var translate = new TranslateTransform()
            //{

            //};
            //tri.RenderTransform = translate;
            //translate.SetValue(FrameworkElement.NameProperty, "Testy");
            //this.RenderTransform = translate;

            //Canvas.SetZIndex(tri, 60);

        }

        private void Tri_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            t = false;
            //throw new NotImplementedException();
        }

        bool t;
        private void Tri_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            t = false;
            //throw new NotImplementedException();
        }

        private void Tri_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (t)
            {
                var ty = Canvas.Children.Where(x => x.GetType() == typeof(Polygon)).FirstOrDefault();

                var gf = Canvas.GetLeft(ty);
                ;
                var post = e.GetCurrentPoint(ty).Position.X;
                //    var posM = e.GetCurrentPoint(this).Position.X;

                if (ty != null)
                {
                    var curpos = e.GetCurrentPoint(this).Position.X;
                    Canvas.SetLeft(ty, curpos);
                }
            }
            //throw new NotImplementedException();

            //var y = ty.RenderTransform;


        }

        private void Tri_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            t = true;
            e.Handled = true;
            //throw new NotImplementedException();

            //(e.OriginalSource as Polygon).Translation = new System.Numerics.Vector3();

            //var ty = Canvas.Children.Where(x => x.GetType() == typeof(Polygon)).FirstOrDefault();
            //(ty.RenderTransform as TranslateTransform).X += 2;
        }

        private void ClearScores()
        {
            Canvas.Children.Clear();
        }

        private void DeleteScore()
        {
            //var ss= GetScore()
            //_scores.Remove()
        }

        //private void DrawRectangle(Score score)
        //{
        //    var rect = new Rectangle();

        //    rect.Width = cellWidth * score.Length;
        //    rect.Height = cellHeight;
        //    rect.Fill = new SolidColorBrush(Windows.UI.Colors.LightGray);
        //    rect.PointerPressed += Rect_PointerPressed;
        //    Canvas.Children.Add(rect);
        //    rect.RadiusX = 2;
        //    rect.RadiusY = 2;
        //    rect.StrokeThickness = 2;
        //    rect.Stroke = new SolidColorBrush(Windows.UI.Colors.Green);
        //    //var x = e.GetCurrentPoint(this).Position.X;
        //    //var y = e.GetCurrentPoint(this).Position.Y;

        //    //var posX = (int)x / 50;
        //    //var posY = (int)y / 20;

        //    Canvas.SetLeft(rect, score.Column * cellWidth);
        //    Canvas.SetTop(rect, score.Row * cellHeight);
        //    Canvas.SetZIndex(rect, 50);
        //}

        private Rectangle DrawRectangle(Score score, Rectangle tempRectangle = null)
        {
            if (score == null || score.Length < 1)
            {
                return null; //TODO Change this behaviour
            }

            ScoreChanged?.Invoke(this, EventArgs.Empty);
            if (tempRectangle == null || !Canvas.Children.Contains(tempRectangle))
            {
                var rect = new Rectangle();

                rect.Width = cellWidth * score.Length;
                rect.Height = cellHeight;
                rect.Fill = new SolidColorBrush(Colors.LightGreen);
                rect.PointerPressed += Rect_PointerPressed;
                rect.RadiusX = 2;
                rect.RadiusY = 2;
                rect.StrokeThickness = 1;
                rect.Stroke = new SolidColorBrush(Colors.Green);

                Canvas.Children.Add(rect);

                Canvas.SetLeft(rect, score.Column * cellWidth + firstColumnWidth);
                Canvas.SetTop(rect, score.Row * cellHeight);
                Canvas.SetZIndex(rect, 50);
                return rect;
            }
            else
            {
                if (score.Length > 0)  //TODO Change this
                {
                    tempRectangle.Width = cellWidth * score.Length;
                }

                //ScoreChanged?.Invoke(this, EventArgs.Empty);

                return tempRectangle;
            }


        }

        private void LoadScore()
        {
            foreach (var score in _scores)
            {
                DrawRectangle(score);
            }
        }

        private bool isMousePressed = false;
        private PointerPoint initPoint;
        //private bool drawn = false;
        //private int? tempScoreCounter;
        Rectangle tempRectangle;
        Score tempScore;
        private void Canvas_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (isMousePressed)
            {
                var point = e.GetCurrentPoint(Canvas);

                var currentX = point.Position.X;
                var initX = initPoint.Position.X;

                var integerInitX = ((int)(initX / cellWidth)) * cellWidth;

                var counter = (int)((currentX - integerInitX) / cellWidth) + 1;

                if (tempScore != null && tempScore.Length != counter)
                {
                    var candidateScore = GetScore(initPoint, counter);
                    if (IsCellAvailable(candidateScore))
                    {
                        tempScore = GetScore(initPoint, counter);
                        tempRectangle = DrawRectangle(tempScore, tempRectangle);
                    }
                }
            }
        }

        private bool IsCellAvailable(Score score)
        {
            var isValid = !this
                .GetTrack(this._trackId)
                .Scores
                .Any(x => score.Row == x.Row && (score.Column < x.Column && (score.Column + score.Length - 1) >= x.Column));

            return isValid;
        }

        private void Canvas_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (isMousePressed)
            {
                if (tempScore != null)
                {
                    this.GetTrack(_trackId).Scores.Add(tempScore);
                }

                isMousePressed = false;
                initPoint = null;
                //tempScoreCounter = null;
                tempRectangle = null;
                tempScore = null;
            }
        }
    }
}
