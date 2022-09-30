using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WeirdViewer
{

    /// <summary>
    /// Interaction logic for WimpyViewer.xaml
    /// </summary>
    public partial class WimpyViewer : Window, INotifyPropertyChanged
    {
        WimpyScene wimpy;
        Spritesheet[] sprites;

        ImageSource getSprite(string spritename)
        {
            foreach (var spritesheet in sprites)
            {
                if (spritesheet.ContainsSprite(spritename)) return spritesheet.GetSprite(spritename)?.Bitmap;
            }
            Console.WriteLine($"WARNING: Sprite {spritename} not found!");
            return null;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public List<ParallaxLayer> Layers { get; set; } = new List<ParallaxLayer>();

        public class ParallaxLayer : INotifyPropertyChanged
        {
            public string Name { get; set; }
            public List<ImageSource> background = new List<ImageSource>();
            public List<SceneObject> Objects = new List<SceneObject>();
            public Vector2 Parallax = new Vector2(0, 0);
            public Vector2 BackgroundOffset = new Vector2(0, 0);
            public Grid UIRepresentation;

            public int zorder;


            public bool IsVisible
            {
                get => UIRepresentation.Visibility == Visibility.Visible;
                set
                {
                    UIRepresentation.Visibility = value ? Visibility.Visible : Visibility.Hidden;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsVisible)));
                }
            }
            public event PropertyChangedEventHandler PropertyChanged;

        }

        public abstract class SceneObject
        {
            public WimpyScene Scene;
            public int zorder;
            public abstract UIElement CreateVisual();
            public abstract bool PixelCoordinateSystem();

            public SceneObject(WimpyScene scene)
            {
                this.Scene = scene;
            }
        }

        public class ImageObject : SceneObject
        {
            public ImageSource source;
            public int posX = 0, posY = 0;
            public string name;

            public ImageObject(WimpyScene scene) : base(scene)
            { }

            public override UIElement CreateVisual()
            {
                return new Image()
                {
                    Source = source,
                    Margin = new Thickness(posX, posY, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    Stretch = Stretch.None,
                };
            }

            public override bool PixelCoordinateSystem() => true;
        }

        public class PolygonObject : SceneObject
        {
            public List<Vector2> Points = new List<Vector2>();
            public Brush color = Brushes.Yellow;

            public PolygonObject(WimpyScene scene) : base(scene)
            { }

            public override UIElement CreateVisual()
            {
                PointCollection polygonPoints = new PointCollection();

                Polygon visual = new Polygon
                {
                    Stroke = color,
                    Fill = Brushes.Transparent,
                    StrokeThickness = 2,
                    Points = polygonPoints
                };

                foreach (var point in Points)
                {
                    polygonPoints.Add(new Point(point.X, Scene.RoomSize.Y - point.Y));
                }
                return visual;
            }

            public override bool PixelCoordinateSystem() => false;
        }

        public class IconObject : SceneObject
        {
            public Vector2 Position;
            public string Name;
            public ImageSource icon;

            public IconObject(WimpyScene scene) : base(scene)
            { }

            public override UIElement CreateVisual()
            {
                Grid g = new Grid();
                Image img = new Image()
                {
                    Source = icon,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    Stretch = Stretch.None,
                    Margin = new Thickness(Position.X - icon.Width / 2, Scene.RoomSize.Y - Position.Y - icon.Height / 2, 0, 0),
                    ToolTip = Name,
                    Effect = new DropShadowEffect() { ShadowDepth = 0 },
                };
                g.Children.Add(img);
                return g;
            }

            public override bool PixelCoordinateSystem() => false;
        }

        public class LightObject : SceneObject
        {
            public ImageSource icon;
            public WimpyScene.SceneLight Light;

            public LightObject(WimpyScene scene) : base(scene)
            { }

            public override UIElement CreateVisual()
            {
                Grid g = new Grid();
                Image icon = new Image()
                {
                    Source = this.icon,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(Light.pos.X - this.icon.Width / 2, Scene.RoomSize.Y - Light.pos.Y - this.icon.Height / 2, 0, 0),
                    Stretch = Stretch.None
                };
                g.Children.Add(icon);
                Ellipse innerRadius = new Ellipse()
                {
                    Width = Light.iradius,
                    Height = Light.iradius,
                    Stroke = Brushes.Red,
                    StrokeThickness = 2,
                    Margin = new Thickness(Light.pos.X - Light.iradius / 2, Scene.RoomSize.Y - Light.pos.Y - Light.iradius / 2, 0, 0),
                    StrokeDashArray = new DoubleCollection(new double[] { 1, 2 }),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                };
                g.Children.Add(innerRadius);
                Ellipse outerRadius = new Ellipse()
                {
                    Width = Light.oradius,
                    Height = Light.oradius,
                    Stroke = Brushes.Red,
                    StrokeThickness = 2,
                    Margin = new Thickness(Light.pos.X - Light.oradius / 2, Scene.RoomSize.Y - Light.pos.Y - Light.oradius / 2, 0, 0),
                    StrokeDashArray = new DoubleCollection(new double[] { 1, 4 }),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                };
                g.Children.Add(outerRadius);

                Ellipse color = new Ellipse()
                {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    Width = 20,
                    Height = 20,
                    Margin = new Thickness(Light.pos.X - 20.0 / 2, Scene.RoomSize.Y - Light.pos.Y - 20.0 / 2, 0, 0),
                    Fill = new SolidColorBrush() { Color = Color.FromArgb(Light.color1.A, Light.color1.R, Light.color1.G, Light.color1.B) },
                };
                g.Children.Add(color);

                return g;
            }

            public override bool PixelCoordinateSystem() => false;
        }

        public WimpyViewer(WimpyScene wimpy, Spritesheet[] sprites)
        {
            InitializeComponent();
            this.DataContext = this;

            this.wimpy = wimpy;
            this.sprites = sprites;

            Title = wimpy.Name;

            double actualBackgroundWidth = 100;

            // Setup parallax layers:
            // Background-layers:
            foreach (var backgroundLayer in wimpy.Layers)
            {
                ParallaxLayer layer = new ParallaxLayer()
                {
                    background = backgroundLayer.Name.Select(s => getSprite(s)).ToList(),
                    Parallax = backgroundLayer.Parallax,
                    zorder = backgroundLayer.ZSort,
                    Name = backgroundLayer.Name.FirstOrDefault() ?? "",
                };

                Layers.Add(layer);
            }

            // Main Layer
            {
                ParallaxLayer layer = new ParallaxLayer()
                {
                    Name = "Play Area",
                    background = wimpy.Background.Select(s => getSprite(s)).ToList(),
                    zorder = 0,
                    Parallax = new Vector2(1, 1),
                };

                actualBackgroundWidth = 0;
                foreach (var bg in layer.background) actualBackgroundWidth += bg?.Width ?? 0;

                foreach (var walkbox in wimpy.WalkBoxes)
                {
                    PolygonObject po = new PolygonObject(wimpy)
                    {
                        zorder = int.MinValue,
                        Points = walkbox.Points,
                        color = Brushes.Blue,
                    };
                    layer.Objects.Add(po);
                }

                foreach (var sceneObject in wimpy.Objects)
                {
                    if (sceneObject.spot > 0)
                    {
                        layer.Objects.Add(new IconObject(wimpy) { zorder = int.MinValue, Name = sceneObject.name, Position = sceneObject.pos, icon = Resources["iconSpot"] as ImageSource });
                    }
                    else if (sceneObject.spine > 0)
                    {
                        // Spine
                        Console.WriteLine($"{sceneObject.name} - Todo: Spinefiles not supported yet!");
                    }
                    else if (sceneObject.emitter > 0)
                    {
                        layer.Objects.Add(new IconObject(wimpy) { zorder = int.MinValue, Name = sceneObject.name, Position = sceneObject.pos, icon = Resources["iconEmitter"] as ImageSource });
                    }
                    else
                    {
                        List<string> spriteNames = new List<string>();
                        if (sceneObject.animations != null && sceneObject.animations.Count > 0)
                        {
                            foreach (var anim in sceneObject.animations)
                            {
                                foreach (var frame in anim.frames)
                                {
                                    spriteNames.Add(frame);
                                }
                            }
                        }
                        else
                        {
                            spriteNames.Add(sceneObject.name);
                        }

                        foreach (var spriteName in spriteNames)
                        {
                            var image = getSprite(spriteName);
                            if (image != null)
                            {
                                ImageObject io = new ImageObject(wimpy)
                                {
                                    source = image,
                                    zorder = sceneObject.zsort,
                                    name = sceneObject.name,
                                };
                                layer.Objects.Add(io);
                            }
                            else
                            {
                                Console.WriteLine($"Image for {sceneObject.name} not found!");
                            }
                        }

                        if (sceneObject.prop == 0)
                        {
                            layer.Objects.Add(new PolygonObject(wimpy) { Points = sceneObject.hotspot.Select(s => s + sceneObject.pos).ToList(), zorder = int.MinValue });
                            Vector2 average = new Vector2(0, 0);
                            foreach (var hotspot in sceneObject.hotspot)
                                average += hotspot;
                            average /= sceneObject.hotspot.Length;
                            layer.Objects.Add(new IconObject(wimpy) { zorder = int.MinValue, Name = sceneObject.name, Position = average + sceneObject.pos, icon = Resources["iconHotspot"] as ImageSource });
                        }
                    }

                }


                foreach (var light in wimpy.Lights)
                {
                    layer.Objects.Add(new LightObject(wimpy) { icon = Resources["iconLight"] as ImageSource, Light = light, zorder = int.MinValue });
                }


                Layers.Add(layer);
            }

            double NonPixelToPixelScale = actualBackgroundWidth / wimpy.RoomSize.X;
            double NonPixelToPixelOffsetX = NonPixelToPixelScale * wimpy.RoomBounds[0].X;
            double NonPixelToPixelOffsetY = NonPixelToPixelScale * wimpy.RoomBounds[0].Y;

            Layers = Layers.OrderByDescending(l => l.zorder).ToList();
            foreach (var layer in Layers)
            {
                Grid g = new Grid();
                layer.UIRepresentation = g;

                host.Children.Add(g);

                double totalBackgroundWidths = 0;
                foreach(var bgimage in layer.background)
                {
                    Image backgroundImage = new Image()
                    {
                        Source = bgimage,
                        Margin = new Thickness(layer.BackgroundOffset.X + totalBackgroundWidths, layer.BackgroundOffset.Y, 0, 0),
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Top,
                        Stretch = Stretch.None,
                    };
                    g.Children.Add(backgroundImage);
                    totalBackgroundWidths += bgimage?.Width ?? 0;
                }

                foreach (var io in layer.Objects.OrderByDescending(o => o.zorder))
                {
                    var vis = io.CreateVisual();
                    if (!io.PixelCoordinateSystem())
                    {
                        if (vis is FrameworkElement fe)
                        {
                            fe.LayoutTransform = new ScaleTransform(NonPixelToPixelScale, NonPixelToPixelScale);
                        }
                    }
                    g.Children.Add(vis);
                }

                g.LayoutTransform = new ScaleTransform(1, 1);
            }

            lblXOffset.Text = lblYOffset.Text = "0.0";
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Layers)));
        }


        private void setScrollLayers(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        {
            foreach (var layer in Layers)
            {
                layer.UIRepresentation.Margin = new Thickness(-(layer.Parallax.X * horizontalScrollBar.Value), -(layer.Parallax.Y * verticalScrollBar.Value), 0, 0);
            }

            lblXOffset.Text = Math.Round(horizontalScrollBar.Value).ToString();
            lblYOffset.Text = Math.Round(verticalScrollBar.Value).ToString();
        }

        private void sceneSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!double.IsNaN(ScenePanel.ActualWidth))
            {
                double width = ScenePanel.ActualWidth - 5;
                double roomWidth = wimpy.RoomBounds[1].X;

                horizontalScrollBar.Minimum = -(width / 2.0);
                horizontalScrollBar.Maximum = roomWidth;
                horizontalScrollBar.ViewportSize = width;
            }
            if (!double.IsNaN(ScenePanel.ActualHeight))
            {
                double height = ScenePanel.ActualHeight - 5;
                double roomHeight = wimpy.RoomBounds[1].Y;

                verticalScrollBar.Minimum = -(height / 2.0);
                verticalScrollBar.Maximum = roomHeight;
                verticalScrollBar.ViewportSize = height;
            }
        }

        private void scrollSceneWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) verticalScrollBar.Value -= e.Delta;
            else horizontalScrollBar.Value += e.Delta;

            setScrollLayers(this, null);

        }

        private void changeLayerVisibility(object sender, RoutedEventArgs e)
        {
            var layer = ((sender as FrameworkElement)?.DataContext as ParallaxLayer);
            if (layer == null) return;
            layer.IsVisible = !layer.IsVisible;
        }
    }
}
