﻿using Sm4shFileExplorer;
using Sm4shFileExplorer.Globals;
using System;
using System.IO;

namespace Sm4shMusic.Objects
{
    public class MyMusicFile
    {
        private const int HEADER_LEN = 0x8;
        public SoundEntryCollection SoundEntryCollection { get; set; }
        private string _Path;
        private byte[] _Header;

        public MyMusicFile(Sm4shProject projectManager, SoundEntryCollection sEntryCollection, string path)
        {
            string mymusicFile = projectManager.ExtractResource(path, PathHelper.FolderTemp);
            SoundEntryCollection = sEntryCollection;
            _Path = path;

            using (FileStream fileStream = File.Open(mymusicFile, FileMode.Open))
            {
                using (BinaryReader b = new BinaryReader(fileStream))
                {
                    if (new string(b.ReadChars(3)) != "MMB")
                        throw new Exception(string.Format("Can't load '{0}', the file is either compressed or its not a MMB file.", mymusicFile));

                    //Keep header
                    b.BaseStream.Position = 0;
                    _Header = b.ReadBytes(HEADER_LEN);

                    //Number of elements
                    b.BaseStream.Position = HEADER_LEN;
                    int nbrStages = b.ReadInt32();

                    //Offsets per element
                    int[] offsetsPerElement = new int[nbrStages];
                    for (int i = 0; i < nbrStages; i++)
                        offsetsPerElement[i] = b.ReadInt32();

                    //Stage offset
                    int stageStartOffset = HEADER_LEN + 0x4 + (nbrStages * 0x4) + 0x20; //0x20 = Magic Padding

                    for (int i = 0; i < nbrStages; i++)
                    {
                        MyMusicStage myMusicStage = new MyMusicStage(i);
                        int currentStageOffset = stageStartOffset + offsetsPerElement[i];

                        b.BaseStream.Position = currentStageOffset;
                        uint nbrBGMs = b.ReadUInt32();

                        currentStageOffset += 0x4;
                        for (int j = 0; j < nbrBGMs; j++)
                        {
                            b.BaseStream.Position = currentStageOffset + (j * 0x3c);
                            myMusicStage.BGMs.Add(ParseBGMEntry(b));
                        }
                        SoundEntryCollection.MyMusicStages.Add(myMusicStage);
                    }
                }
            }
        }

        public void BuildFile()
        {
            string savePath = PathHelper.GetWorkplaceFolder(PathHelperEnum.FOLDER_PATCH) + _Path.Replace('/', Path.DirectorySeparatorChar);

            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter w = new BinaryWriter(stream))
                {
                    w.Write(_Header);

                    //NbrStage
                    w.Write(SoundEntryCollection.MyMusicStages.Count);

                    uint offSet = 0;
                    foreach (MyMusicStage myMusicStage in SoundEntryCollection.MyMusicStages)
                    {
                        w.Write(offSet);
                        offSet += ((uint)myMusicStage.BGMs.Count * 0x3c) + 0x24; //0x24 = Padding + Count
                    }

                    while ((w.BaseStream.Position) % 0x3c != 0) //Magic?
                        w.Write((byte)0x0);

                    for(int i = 0; i < SoundEntryCollection.MyMusicStages.Count; i++)
                    {
                        MyMusicStage myMusicStage = SoundEntryCollection.MyMusicStages[i];
                        w.Write(myMusicStage.BGMs.Count);
                        foreach (MyMusicStageBGM myMusicStageBGM in myMusicStage.BGMs)
                        {
                            WriteBGMEntry(w, myMusicStageBGM);
                        }
                        if (i + 1 != SoundEntryCollection.MyMusicStages.Count)
                        {
                            for (int j = 0; j < 0x20; j++)  //Padding
                                w.Write((byte)0x0);
                        }
                    }
                }

                Directory.CreateDirectory(Path.GetDirectoryName(savePath));
                File.WriteAllBytes(savePath, stream.ToArray());
            }
        }

        private MyMusicStageBGM ParseBGMEntry(BinaryReader b)
        {
            int BGMID = b.ReadInt32();

            MyMusicStageBGM sMyMusicBGM = new MyMusicStageBGM(SoundEntryCollection, BGMID);

            sMyMusicBGM.Index = b.ReadInt16();
            sMyMusicBGM.SubIndex = b.ReadInt16();
            sMyMusicBGM.Rarity = b.ReadInt32();
            sMyMusicBGM.Unk3 = b.ReadInt16();
            sMyMusicBGM.Unk4 = b.ReadInt16();
            sMyMusicBGM.PlayDelay = b.ReadInt32();
            sMyMusicBGM.Unk6 = b.ReadInt32();
            sMyMusicBGM.Unk7 = b.ReadInt32();
            sMyMusicBGM.Unk8 = b.ReadInt32();
            sMyMusicBGM.Unk9 = b.ReadInt32();

            return sMyMusicBGM;
        }

        private void WriteBGMEntry(BinaryWriter w, MyMusicStageBGM myMusicStageBGM)
        {
            w.Write(myMusicStageBGM.BGMID);
            w.Write(myMusicStageBGM.Index);
            w.Write(myMusicStageBGM.SubIndex);
            w.Write(myMusicStageBGM.Rarity);
            w.Write(myMusicStageBGM.Unk3);
            w.Write(myMusicStageBGM.Unk4);
            w.Write(myMusicStageBGM.PlayDelay);
            w.Write(myMusicStageBGM.Unk6);
            w.Write(myMusicStageBGM.Unk7);
            w.Write(myMusicStageBGM.Unk8);
            w.Write(myMusicStageBGM.Unk9);

            for (int i = 0; i < 0x18; i++) //Padding??
                w.Write((byte)0x0);
        }
    }
}
