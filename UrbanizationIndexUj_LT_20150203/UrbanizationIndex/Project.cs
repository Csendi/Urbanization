using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace UrbanizationIndex
{
    public class Project
    {
        public enum PCAStates { OK, NoPCA, BadPCA, NeedsRecalc };
        public enum SVMStates { Saved, Modified }
        public enum TerritoryStates { Saved, Modified }

        private CultureInfo ci;
        public string Path { get; private set; }
        public string Name { get; set; }
        public string DataPath { get; private set; }
        public string ProjectFilePath { get; private set; }
        public Dictionary<int, Territory> Territories { get; set; }
        public PCAStates PCAState { get; set; }
        public int NextTerritoryID { get; set; }
        public SVMStates SVMState { get; set; }
        public TerritoryStates TerritoryState { get; set; }

        public static Project Load(string projectFilePath)
        {
            return new Project(projectFilePath);
        }


        private Project(string projectFilePath)
        {
            Territories = new Dictionary<int, Territory>();

            FileInfo info = new FileInfo(projectFilePath);
            Path = info.DirectoryName;
            Name = info.Name.Substring(0, info.Name.Length - info.Extension.Length);
            ProjectFilePath = projectFilePath;
            DataPath = Path + '\\' + Name;
            NextTerritoryID = 1;
            TerritoryState = TerritoryStates.Saved;

            if (!Directory.Exists(DataPath))
                Directory.CreateDirectory(DataPath);

            ci = CultureInfo.InvariantCulture.Clone() as CultureInfo;
            ci.NumberFormat.NumberDecimalSeparator = "."; // force program using . as separator

            string[] lines = File.ReadAllLines(ProjectFilePath);
            foreach (string line in lines)
            {
                List<string> data = CsvHelper.Separate(line, ';');
                int id = int.Parse(data[0]);
                data[1] = data[1].Replace(',', '.');
                data[2] = data[2].Replace(',', '.');
                Territories.Add(id, new Territory(id, double.Parse(data[1], ci), double.Parse(data[2], ci), data[3], DataPath));

                if (id >= NextTerritoryID)
                    NextTerritoryID = id + 1;
            }

            LoadSVMResults();
            LoadPCAResults();
        }

        public void ReCalculateNextTerrID()
        {
            NextTerritoryID = 1;
            foreach (var item in Territories)
            {
                if (item.Value.ID >= NextTerritoryID)
                    NextTerritoryID = item.Value.ID + 1;
            }
        }

        public void LoadSVMResults()
        {
            SVMState = SVMStates.Saved;

            if (File.Exists(DataPath + "\\svmResult.csv"))
            {
                String[] SVMLines = File.ReadAllLines(DataPath + "\\svmResult.csv");
                for (int i = 1; i < SVMLines.Length; i++)
                {
                    string[] data = SVMLines[i].Split(new char[] { ';' });

                    try
                    {
                        int last = data[0].LastIndexOf('\\');
                        int from = data[0].LastIndexOf('\\', last - 1) + 1;
                        int id = int.Parse(data[0].Substring(from, last - from));
                        int svmID = int.Parse(data[1]);

                        SVMResult res = Territories[id].SVMResults[svmID];
                        res.Building = int.Parse(data[2]);
                        res.Vegetation = int.Parse(data[3]);
                        res.Road = int.Parse(data[4]);
                    }
                    catch (Exception) { }
                }
            }
        }

        public void LoadPCAResults()
        {
            PCAState = PCAStates.OK;

            if (!File.Exists(DataPath + "\\pcaResult.csv"))
            {
                PCAState = PCAStates.NoPCA;
                return;
            }

            String[] PCALines = File.ReadAllLines(DataPath + "\\pcaResult.csv");

            if (PCALines.Length - 1 != Territories.Count)
                PCAState = PCAStates.NeedsRecalc;


            bool NoNames = false;
            for (int i = 1; i < PCALines.Length; i++)
            {
                string[] data = PCALines[i].Split(new char[] { ';' });
                if (data.Length != 8)
                    NoNames = true;

                try
                {
                    int last = data[0].LastIndexOf('\\');
                    int from = data[0].LastIndexOf('\\', last - 1) + 1;
                    int id = int.Parse(data[0].Substring(from, last - from));
                    Territories[id].PCAResult = double.Parse(data[6].Replace(',', '.'), ci);
                }
                catch (FormatException)
                {
                    PCAState = PCAStates.BadPCA;
                    break;
                }
                catch (Exception) { }
            }

            if (NoNames)
                SavePCA();
        }

        private void SaveSVM()
        {
            using (StreamWriter wr = new StreamWriter(DataPath + "\\svmResult.csv"))
            {
                wr.WriteLine("Picture_name;Cell_name;Building_label;Vegetation_label;Road_label");

                foreach (var item in Territories)
                {
                    SVMResult[] svmResults = item.Value.SVMResults;
                    for (int i = 0; i < svmResults.Length; i++)
                    {
                        wr.WriteLine(item.Value.DirPath + "\\01.bmp;" + i.ToString() + ";" + svmResults[i].Building.ToString() + ";" + svmResults[i].Vegetation.ToString() + ";" + svmResults[i].Road.ToString());
                    }
                }
            }
            SVMState = SVMStates.Saved;
        }

        // Ennek csak annyi a feladata, hogy beírja a terület neveket az utolsó oszlopba és az első sorát a fájlnak felülírja angolul
        private void SavePCA()
        {
            if (!File.Exists(DataPath + "\\pcaResult.csv"))
                return;

            string[] pcaLines = File.ReadAllLines(DataPath + "\\pcaResult.csv");

            using (StreamWriter wr = new StreamWriter(DataPath + "\\pcaResult.csv"))
            {
                wr.WriteLine("Picture_name;Cells_where_building_label=2;Cells_where_vegetation_label=2;Cells_where_road_label=1;Avg_building_labels;Avg_vegetation_labels;PCA_value;Location_name");

                int i = 1;
                foreach (var item in Territories)
                {
                    string line = pcaLines[i++];
                    string[] data = line.Split(new char[] { ';' });
                    int last = data[0].LastIndexOf('\\');
                    int from = data[0].LastIndexOf('\\', last - 1) + 1;
                    int id = int.Parse(data[0].Substring(from, last - from));
                    wr.WriteLine(line + ';' + CsvHelper.ToCsvData(Territories[id].Name));
                    //wr.WriteLine(line + ';' + CsvHelper.ToCsvData(item.Value.Name));
                }
            }
        }

        private void SaveTerritories()
        {
            using (StreamWriter projFileWr = new StreamWriter(ProjectFilePath))
            {
                foreach (KeyValuePair<int, Territory> item in Territories)
                {
                    projFileWr.WriteLine(item.Key.ToString() + ';' + item.Value.Latitude + ';' + item.Value.Longitude + ';' + CsvHelper.ToCsvData(item.Value.Name));

                    if (item.Value.TrainingPoints.Count != 0)
                    {
                        using (StreamWriter file = new StreamWriter(DataPath + "\\" + item.Key.ToString() + "\\03.txt"))
                        {
                            foreach (TrainingPoint tp in item.Value.TrainingPoints)
                                file.WriteLine(tp.XPos.ToString() + " " + tp.YPos.ToString() + " " + tp.Alias);
                        }
                    }
                }
            }
            TerritoryState = TerritoryStates.Saved;
        }

        public void Save()
        {
            if (TerritoryState == TerritoryStates.Modified)
                SaveTerritories();

            if (SVMState == SVMStates.Modified)
                SaveSVM();
        }
    }
}
