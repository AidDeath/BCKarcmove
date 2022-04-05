using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Mime;
using System.Text;

namespace BCKarcmove
{
    class ImageGenerator
    {

        public void GetImageForGu(List<ArchiveFile> archivesFiles)
        {
            var full = Image.FromFile(@"pictures\gu_blank.png");
            var rarIcon = Image.FromFile((@"pictures/winraricon.png"));
            var secondaryColor = ColorTranslator.FromHtml("#6d6d6d");

            using (var gfx = Graphics.FromImage(full))
            {
                var yOffset = 0;
                foreach (var archiveFile in archivesFiles)
                {
                    gfx.DrawImage(rarIcon, 250, 223 + yOffset);
                    gfx.DrawImage(DrawTextImage(archiveFile.FileName, minSize: new Size(230, 18)), 250 + rarIcon.Width, 225 + yOffset);
                    gfx.DrawImage(DrawTextImage(archiveFile.ChangeDate.ToString(), textColorOptional: secondaryColor), 510, 225 + yOffset);
                    gfx.DrawImage(DrawTextImage(archiveFile.Type, textColorOptional: secondaryColor), 655, 225 + yOffset);
                    gfx.DrawImage(DrawTextImage($"{archiveFile.SizeInKb} КБ", textColorOptional: secondaryColor), 771, 225 + yOffset);


                    gfx.Save();

                    yOffset += 18;
                }
            }
            
            full.Save(@"pictures/output.png");
           
        }



        // Вызов рисования картинки
        private Image DrawTextImage(String text, Font fontOptional = null, Color? textColorOptional = null, Color? backColorOptional = null, Size? minSize = null)
        {
            Font font = fontOptional ?? SystemFonts.DefaultFont;
            Color textColor = textColorOptional ?? Color.Black;
            Color backColor = backColorOptional ?? Color.White;
            Size minimalSize = minSize ?? new Size(0, 18); 

            return DrawTextImage(text, font, textColor, backColor, minimalSize);
        }

        //Рисуем картинку из текста
        private Image DrawTextImage(String text, Font font, Color textColor, Color backColor, Size minSize)
        {
            //first, create a dummy bitmap just to get a graphics object
            SizeF textSize;
            using (Image img = new Bitmap(1, 1))
            {
                using (Graphics drawing = Graphics.FromImage(img))
                {
                    //measure the string to see how big the image needs to be
                    textSize = drawing.MeasureString(text, font);
                    if (!minSize.IsEmpty)
                    {
                        textSize.Width = textSize.Width > minSize.Width ? textSize.Width : minSize.Width;
                        textSize.Height = textSize.Height > minSize.Height ? textSize.Height : minSize.Height;
                    }
                }
            }

            //create a new image of the right size
            Image retImg = new Bitmap((int)textSize.Width, (int)textSize.Height);
            using (var drawing = Graphics.FromImage(retImg))
            {
                //paint the background
                drawing.Clear(backColor);

                //create a brush for the text
                using (Brush textBrush = new SolidBrush(textColor))
                {
                    drawing.DrawString(text, font, textBrush, 0, 0);
                    drawing.Save();
                }
            }
            return retImg;
        }
    }
}
