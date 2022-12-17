using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Nebula.Module
{
    public static class DllInstaller
    {
        public static async Task DownloadDll(string name)
        {
            if (!Directory.Exists("Dll")) Directory.CreateDirectory("Dll");
            if (!File.Exists("Dll/"+name+".dll"))
            {
                HttpClient http = new HttpClient();
                var response = await http.GetAsync(new System.Uri("https://raw.githubusercontent.com/Dolly1016/Nebula/master/Dll/"+name+".dll"), HttpCompletionOption.ResponseContentRead);
                if (response.StatusCode != HttpStatusCode.OK || response.Content == null)
                {
                    return;
                }

                using (var responseStream = await response.Content.ReadAsStreamAsync())
                {
                    using (var fileStream = File.Create("Dll/"+ name + ".dll"))
                    {
                        responseStream.CopyTo(fileStream);
                    }
                }
            }
        }
    }
}
