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


        public void mainCall(string add)
        {
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", "C:\\Users\\livei\\Downloads\\text-to-speech-312622-3fe96c3d67ea.json");

            if (add.Contains("archiveofourown.org/works/"))
            {
                //Console.WriteLine(args[0]);
                if (!doneFics.Contains(add))
                {
                    doneFics.Add(add);
                    address = add;
                    test(address);
                }
            }

            //"https://archiveofourown.org/works/30950015";
            //"https://archiveofourown.org/works/19762567/chapters/46780885"; - chapters, not working rn
            //"https://archiveofourown.org/works/30950222";   -odd paragraphs
            //"https://archiveofourown.org/works/30247587/chapters/74541246#workskin";

            //String ad = "https://archiveofourown.org/works/16599134";
            //address = ad;
            //List<string> ee = test(ad);

        }

        private List<string> test(string e)
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

            //this line might be unneeded
            //toRet = concat(toRet);e


            //int j = 0;
            ////Console.WriteLine("This is a test");
            //foreach (string r in toRet)
            //{
            //    j++;
            //    Console.WriteLine(j + "  " + r);
            //}

            return toRet;
        }

        private string findTitle(string html)
        {
            doc = new HtmlDocument();
            doc.LoadHtml(html);
            HtmlNode node = doc.DocumentNode.SelectSingleNode("//div[@id='workskin']");
            HtmlNode node2 = node.SelectSingleNode("//div[@class='preface group']");
            //Console.WriteLine(node2.InnerText);
            HtmlNode node3 = node2.SelectSingleNode("//h2[@class='title heading']");

            //HtmlNode node3 = node2.Descendants().Where(n => n.HasClass("title heading")).First();
            return node3.InnerText.Trim();
            //return null;
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
            //Console.WriteLine(node.OuterHtml);
            HtmlNode head = fork(node);
            //IEnumerable<HtmlNode> nodes = doc.DocumentNode.Descendants().Where(n => n.HasClass("chapter"));

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
            
            if (toRet.Length > 1)
            {
                
                using (File.Create(finalTitle)) ;
                FileStream fs = new FileStream(finalTitle, FileMode.Open);
                Combine(toRet, fs);

                //for (int j = toRet.Length - 1; j >= 0; j--)
                //{
                //    File.Delete(toRet[j]);
                //}
            }
            else
            {
                new System.IO.FileInfo(toRet[0]).MoveTo(finalTitle);
            }

            return strHold;
        }
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
                }
                Console.WriteLine("Audio content written to file \"" + number + "output.mp3\"");
                return currentDir + "\\" + number + "output.mp3";
            }
        }

        private void Combine(string[] inputFiles, Stream output)
        {
            foreach (string file in inputFiles)
            {
                Mp3FileReader reader = new Mp3FileReader(file);
                if ((output.Position == 0) && (reader.Id3v2Tag != null))
                {
                    output.Write(reader.Id3v2Tag.RawData, 0, reader.Id3v2Tag.RawData.Length);
                }
                Mp3Frame frame;
                while ((frame = reader.ReadNextFrame()) != null)
                {
                    output.Write(frame.RawData, 0, frame.RawData.Length);
                }
            }
        }
    }

}
