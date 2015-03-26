using System.Drawing;

namespace UrbanizationIndex
{
    public class TrainingPoint
    {
        public int XPos { get; set; }
        public int YPos { get; set; }
        public string Name { get; private set; }
        public string Alias { get; private set; }

        public TrainingPoint(int xpos, int ypos, string name, bool nameisalias = false)
        {
            XPos = xpos;
            YPos = ypos;

            if (!nameisalias)
            {
                Name = name;

                switch (Name)
                {
                    case "Forest":
                        Alias = "v_r";
                        break;
                    case "Vegetation (grass, crop field)":
                        Alias = "v_u";
                        break;
                    case "Building":
                        Alias = "b";
                        break;
                    case "Paved road":
                        Alias = "r";
                        break;
                    case "Other (railway, water, ...)":
                        Alias = "o";
                        break;
                }
            }
            else
            {
                Alias = name;

                switch (Alias)
                {
                    case "v_r":
                        Name = "Forest";
                        break;
                    case "v_u":
                        Name = "Vegetation (grass, crop field)";
                        break;
                    case "b":
                        Name = "Building";
                        break;
                    case "r":
                        Name = "Paved road";
                        break;
                    case "o":
                        Name = "Other (railway, water, ...)";
                        break;
                }
            }
        }

        public override string ToString()
        {
            return XPos.ToString() + " " + YPos.ToString() + " " + Name;
        }

        public static Color GetColorForTP(TrainingPoint tp)
        {
            switch (tp.Alias)
            {
                case "v_r":
                    return Color.Yellow;
                case "v_u":
                    return Color.GreenYellow;
                case "b":
                    return Color.LightBlue;
                case "r":
                    return Color.Pink;
                case "o":
                    return Color.White;
            }
            return Color.Black;
        }
    }
}
