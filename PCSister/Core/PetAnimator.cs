using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Imaging;
using PCSister.Config;

namespace PCSister.Core
{
    public class PetAnimator
    {
        public Dictionary<string, List<BitmapSource>> Sprites { get; private set; }

        public PetAnimator()
        {
            Sprites = new Dictionary<string, List<BitmapSource>>();
            LoadSprites();
        }

        private void LoadSprites()
        {
            // Имя файла для каждого состояния
            var stateFiles = new Dictionary<string, string>
            {
                { Settings.StateIdle,  "Afk"  },
                { Settings.StateIdle2, "Afk2" },
                { Settings.StateWalk,  "Walk" },
                { Settings.StateRun,   "Run"  },
                { Settings.StateJump,  "Jump" },
                { Settings.StateDead,  "Dead" },
                { Settings.StateHurt,  "Hurt" }
            };

            foreach (var kvp in stateFiles)
            {
                string state = kvp.Key;
                string fileName = kvp.Value + ".png";
                Uri uri = new Uri($"pack://application:,,,/PCSister;component/Assets/{fileName}", UriKind.Absolute);

                try
                {
                    BitmapImage sheet = new BitmapImage();
                    sheet.BeginInit();
                    sheet.UriSource = uri;
                    sheet.CacheOption = BitmapCacheOption.OnLoad;
                    sheet.EndInit();
                    sheet.Freeze();

                    List<BitmapSource> frames = new List<BitmapSource>();
                    int count = Settings.FrameCounts[state];

                    for (int i = 0; i < count; i++)
                    {
                        Int32Rect rect = new Int32Rect(i * Settings.SpriteSize, 0, Settings.SpriteSize, Settings.SpriteSize);
                        CroppedBitmap frame = new CroppedBitmap(sheet, rect);
                        frames.Add(frame);
                    }

                    Sprites[state] = frames;
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Стан: {state}\nФайл: {fileName}\nURI: {uri}\n\nПомилка: {ex.Message}", "Помилка завантаження спрайту");
                }
            }
        }
    }
}