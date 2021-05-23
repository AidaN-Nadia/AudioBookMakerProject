using System;
using System.IO;
using System.Net;
using HtmlAgilityPack;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Runtime.InteropServices.ComTypes;
using Google.Cloud.TextToSpeech.V1;
using NAudio.Wave;
using System.Text.RegularExpressions;
using System.Threading;

namespace AudioBookMakerProject
{
    public class Class1
    {
        private string address;
        private HtmlDocument doc;
        private string title;
        private string activeDir;
        private List<string> doneFics = new List<string>();
        private string currentDir;
        private string fileOutput;
        public Class1()
        {
            activeDir = Directory.GetCurrentDirectory();
            currentDir = activeDir;
        }
        public Class1(string folder)
        {
            Directory.SetCurrentDirectory(folder);
            activeDir = Directory.GetCurrentDirectory();
            currentDir = activeDir;
        }


        public string mainCall(string add)
        {
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", "C:\\Users\\livei\\Downloads\\text-to-speech-312622-3fe96c3d67ea.json");

            if (add.Contains("archiveofourown.org/works/"))
            {
                if (!doneFics.Contains(add))
                {
                    doneFics.Add(add);
                    address = add;
                    hubMethod(address);
                }
            }

            // testing links

            //"https://archiveofourown.org/works/30950015";
            //"https://archiveofourown.org/works/19762567/chapters/46780885"; - chapters, not working rn
            //"https://archiveofourown.org/works/30950222";   -odd paragraphs
            //"https://archiveofourown.org/works/30247587/chapters/74541246#workskin";

            //String ad = "https://archiveofourown.org/works/16599134";
            //address = ad;
            //List<string> ee = test(ad);
            return fileOutput;
        }

        private List<string> hubMethod(string e)
        {

            List<string> toRet = new List<string>();
            address = address + "?view_adult=true";
            HttpWebRequest webrequest = (HttpWebRequest)WebRequest.Create(address);
            webrequest.Method = "GET";
            HttpWebResponse webResponse = (HttpWebResponse)webrequest.GetResponse();
            string responseHtml;
            using (StreamReader responseStream = new StreamReader(webResponse.GetResponseStream()))
            {
                responseHtml = responseStream.ReadToEnd().Trim();
            }

            title = findTitle(responseHtml);

            createDir();
            
            toRet = idSplit(responseHtml);

            return toRet;
        }

        private string findTitle(string html)
        {
            doc = new HtmlDocument();
            doc.LoadHtml(html);
            HtmlNode node = doc.DocumentNode.SelectSingleNode("//div[@id='workskin']");
            HtmlNode node2 = node.SelectSingleNode("//div[@class='preface group']");
            HtmlNode node3 = node2.SelectSingleNode("//h2[@class='title heading']");
            return GetValidFileName(node3.InnerText.Trim());
        }
        private string GetValidFileName(string fileName)
        {
            // remove any invalid character from the filename.  
            String ret = Regex.Replace(fileName.Trim(), "[^A-Za-z0-9_. ]+", "");
            return ret.Replace(" ", String.Empty);
        }

        private void createDir()
        {
            Console.WriteLine(activeDir);
            Console.WriteLine(title);
            string tempFolder = activeDir + @"\" + title.Replace(" ", "_");
            Boolean folderExists = true;
            int i = 0;
            while (folderExists)
            {
                if (!Directory.Exists(tempFolder+i))
                {
                    Directory.CreateDirectory(tempFolder+i);
                    currentDir = tempFolder + i;
                    folderExists = false;
                }
                i++;
            }
        }



        //https://dotnetfiddle.net/wA4jnc by Anjan Kant
        private List<string> idSplit(string htmlIn)
        {
            doc = new HtmlDocument();
            doc.LoadHtml(htmlIn);
            HtmlNode node = doc.DocumentNode.SelectSingleNode("//div[@id='workskin']");
            HtmlNode head = fork(node);

            List<string> strHold = new List<string>();
            foreach (var item in head.Descendants())
            {
                if (!(item.HasChildNodes))
                {
                    strHold.Add(item.InnerText);
                }
            }

            strHold = concat(strHold);
            string[] toRet = new string[strHold.Count];
            int i = 0;

            foreach (string s in strHold)
            {
                toRet[i] = (speak(s, i.ToString()));
                i++;
            }
            string finalTitle = currentDir + "\\" + title + ".mp3";
            fileOutput = finalTitle;
            
            if (toRet.Length > 1)
            {
                
                using (File.Create(finalTitle)) ;
                FileStream fs = new FileStream(finalTitle, FileMode.Open);

                Concatenate(fs, toRet);
                //Combine(toRet, fs);

            }
            else
            {
                new System.IO.FileInfo(toRet[0]).MoveTo(finalTitle);
            }

            return strHold;
        }
        
    //may not be needed any more

    //public static bool IsFileLocked(FileInfo file)
    //{
    //    FileStream stream = null;

    //    try
    //    {
    //        stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
    //    }
    //    catch (IOException)
    //    {
    //        return true;
    //    }
    //    finally
    //    {
    //        if (stream != null)
    //            stream.Close();
    //    }
    //    return false;
    //}
    private HtmlNode fork(HtmlNode h)
        {
            if (address.Contains("chapters"))
            {
                return multi(h);
            }
            else
            {
                return single(h);
            }
        }

        private HtmlNode multi(HtmlNode h)
        {
            HtmlNode nodes1 = h.SelectSingleNode("//div[@id='chapters']");
            HtmlNode nodes2 = nodes1.SelectSingleNode("//div[@class='chapter']");
            IEnumerable<HtmlNode> nodes3 = nodes2.Descendants().Where(n => n.HasClass("userstuff"));

            return nodes3.First();
        }
        private HtmlNode single(HtmlNode h)
        {
            HtmlNode nodes1 = h.SelectSingleNode("//div[@id='chapters']");
            IEnumerable<HtmlNode> nodes2 = nodes1.Descendants().Where(n => n.HasClass("userstuff"));
            return nodes2.First();
        }

        private List<string> concat(List<string> l)
        {
            int counter = 0;
            string temp = "";
            List<string> toRet = new List<string>();
            foreach (string s in l)
            {
                if (s.Length > 4990)
                {
                    Console.WriteLine("1");
                    toRet.Add(temp);
                    counter = 0;
                    toRet = toRet.Concat(overLimit(s)).ToList();
                }
                else if (counter + s.Length > 4990)
                {
                    Console.WriteLine("2");

                    toRet.Add(temp);
                    temp = s;
                    counter = s.Length;
                }
                else
                {
                    Console.WriteLine("3");

                    temp += s;
                    counter += s.Length;
                }
            }
            toRet.Add(temp);
            return toRet;
        }

        private List<string> overLimit(string s)
        {
            List<string> ret = new List<string>();
            while (s.Length > 4990)
            {
                ret.Add(s.Substring(0, 4990));
                s.Remove(0, 4990);
            }
            return ret;
        }


        //This is from the google api documentation
        private string speak(string e, string number)
        {
            {
                var client = TextToSpeechClient.Create();

                // The input to be synthesized, can be provided as text or SSML.
                var input = new SynthesisInput
                {
                    Text = e
                    //Text = "This is a demonstration of the Google Cloud Text-to-Speech API"
                };

                // Build the voice request.
                var voiceSelection = new VoiceSelectionParams
                {
                    LanguageCode = "en-US",
                    SsmlGender = SsmlVoiceGender.Female
                    //, Name =
                };

                // Specify the type of audio file.
                var audioConfig = new AudioConfig
                {
                    AudioEncoding = AudioEncoding.Mp3
                };

                // Perform the text-to-speech request.
                var response = client.SynthesizeSpeech(input, voiceSelection, audioConfig);

                // Write the response to the output file.
                using (var output = File.Create(currentDir + "\\" + number + "output.mp3"))
                {

                    response.AudioContent.WriteTo(output);
                    output.Flush();
                    output.Close();
                }
                Console.WriteLine("Audio content written to file \"" + number + "output.mp3\"");
                return currentDir + "\\" + number + "output.mp3";
            }
        }


        //https://stackoverflow.com/questions/8657915/delete-file-after-creation
        public static void Concatenate(Stream output, params string[] mp3filenames)
        {
            foreach (string filename in mp3filenames)
            {
                using (FileStream fs = File.OpenRead(filename))
                {
                    fs.CopyTo(output);
                }
                
            }
            output.Flush();
            output.Close();

            foreach (string filename in mp3filenames)
            {
                try
                {
                    
                    File.Delete(filename);
                    Console.WriteLine(filename+ " was deleted successfully.");
                }
                catch
                {

                    GC.Collect(); //kill object that keep the file. I think dispose will do the trick as well.
                    System.Threading.Thread.Sleep(500); //Wait for object to be killed.
                    File.Delete(filename); //File can be now deleted
                }
                File.Delete(filename);
            }
        }

        //This is an old version that uses the NAudio package, which is not usable on android mobile devices
        
        //private void Combine(string[] inputFiles, Stream output)
        //{
        //    foreach (string file in inputFiles)
        //    {
        //        Mp3FileReader reader = new Mp3FileReader(file);
        //        if ((output.Position == 0) && (reader.Id3v2Tag != null))
        //        {
        //            output.Write(reader.Id3v2Tag.RawData, 0, reader.Id3v2Tag.RawData.Length);
        //        }
        //        Mp3Frame frame;
        //        while ((frame = reader.ReadNextFrame()) != null)
        //        {
        //            output.Write(frame.RawData, 0, frame.RawData.Length);
        //        }
        //    }
        //}
    }

}
