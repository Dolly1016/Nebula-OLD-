using System.Reflection;

namespace Nebula;

public static class DevTools
{

    private static FileStream CreateEmptyWav(string filepath)
    {
        var fileStream = new FileStream(filepath, FileMode.Create);

        return fileStream;
    }

    public static void SaveRelatedSprite(Texture2D texture, string folderName)
    {
        var sprites = UnityEngine.Object.FindObjectsOfTypeIncludingAssets(Il2CppType.Of<Sprite>()).Where((obj) =>
        {
            return obj.CastFast<Sprite>().texture == texture;
        });

        int margin = 10;
        var readable = Helpers.CreateReadabeTexture(texture, margin);

        foreach (var obj in sprites)
        {
            var sprite = obj.CastFast<Sprite>();
            var spriteTex = new Texture2D((int)sprite.rect.width, (int)sprite.rect.height);
            var pixels = readable.GetPixels((int)sprite.rect.x + margin,
                                                    (int)sprite.rect.y + margin,
                                                    (int)sprite.rect.width,
                                                    (int)sprite.rect.height);
            spriteTex.SetPixels(pixels);
            spriteTex.Apply();
            byte[] bytes = UnityEngine.ImageConversion.EncodeToPNG(spriteTex);
            if (!Directory.Exists(folderName)) Directory.CreateDirectory(folderName);
            File.WriteAllBytes(folderName + "/" + sprite.name + ".png", bytes);
        }
    }

    public static void SaveTexture(Texture2D texture, string fileName)
    {
        byte[] bytes = UnityEngine.ImageConversion.EncodeToPNG(Helpers.CreateReadabeTexture(texture));
        //保存
        File.WriteAllBytes(fileName + ".png", bytes);
    }

    public static void SearchAndSaveTextureFromMesh(string objName, string fileName)
    {
        var obj = UnityEngine.GameObject.Find(objName);
        var renderer = obj.GetComponent<MeshRenderer>();
        if (renderer == null) return;

        byte[] bytes = UnityEngine.ImageConversion.EncodeToPNG(Helpers.CreateReadabeTexture(renderer.material.mainTexture));
        //保存
        File.WriteAllBytes(fileName + ".png", bytes);
    }

    public static void SearchAndSaveTexture(string objName, string fileName)
    {
        var obj = UnityEngine.GameObject.Find(objName);
        var renderer = obj.GetComponent<SpriteRenderer>();
        if (renderer == null) return;

        byte[] bytes = UnityEngine.ImageConversion.EncodeToPNG(Helpers.CreateReadabeTexture(renderer.sprite.texture));
        //保存
        File.WriteAllBytes(fileName + ".png", bytes);
    }

    public static void SearchAndSaveTextureFromSprite(string objName, string fileName)
    {
        foreach (var sprite in UnityEngine.Object.FindObjectsOfTypeAll(Il2CppType.Of<Sprite>()))
        {
            if (sprite.name != objName) continue;

            byte[] bytes = UnityEngine.ImageConversion.EncodeToPNG(Helpers.CreateReadabeTexture(sprite.Cast<Sprite>().texture));
            //保存
            File.WriteAllBytes(fileName + ".png", bytes);

            break;
        }
    }

    public static void SaveAllSound(string directory)
    {
        foreach (var audio in UnityEngine.Object.FindObjectsOfTypeIncludingAssets(Il2CppType.Of<AudioClip>()))
        {
            var filepath = directory + "/" + audio.name + ".wav";

            // Make sure directory exists if user is saving to sub dir.
            Directory.CreateDirectory(Path.GetDirectoryName(filepath));

            using (var fileStream = CreateEmptyWav(filepath))
            {
                var clip = audio.Cast<AudioClip>();

                WriteHeader(fileStream, clip);
                ConvertAndWrite(fileStream, clip);
            }

        }
    }

    private static void ConvertAndWrite(FileStream fileStream, AudioClip clip)
    {

        var samples = new float[clip.samples * clip.channels];

        clip.LoadAudioData();

        clip.GetData(samples, 0);

        int rescaleFactor = 32767; //to convert float to Int16

        for (int i = 0; i < samples.Length; i++)
        {
            short shortData = (short)(samples[i] * rescaleFactor);
            Byte[] byteArr = BitConverter.GetBytes(shortData);
            fileStream.Write(byteArr, 0, byteArr.Length);
        }

        clip.UnloadAudioData();
    }

    private static void WriteHeader(FileStream fileStream, AudioClip clip)
    {

        var hz = clip.frequency;
        var channels = clip.channels;
        var samples = clip.samples;

        fileStream.Seek(0, SeekOrigin.Begin);

        Byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
        fileStream.Write(riff, 0, 4);

        Byte[] chunkSize = BitConverter.GetBytes((UInt32)(44 + clip.samples * clip.channels * 2));
        fileStream.Write(chunkSize, 0, 4);

        Byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
        fileStream.Write(wave, 0, 4);

        Byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
        fileStream.Write(fmt, 0, 4);

        Byte[] subChunk1 = BitConverter.GetBytes(16);
        fileStream.Write(subChunk1, 0, 4);

        UInt16 one = 1;

        Byte[] audioFormat = BitConverter.GetBytes(one);
        fileStream.Write(audioFormat, 0, 2);

        Byte[] numChannels = BitConverter.GetBytes(channels);
        fileStream.Write(numChannels, 0, 2);

        Byte[] sampleRate = BitConverter.GetBytes(hz);
        fileStream.Write(sampleRate, 0, 4);

        Byte[] byteRate = BitConverter.GetBytes(hz * channels * 2); // sampleRate * bytesPerSample*number of channels, here 44100*2*2
        fileStream.Write(byteRate, 0, 4);

        UInt16 blockAlign = (ushort)(channels * 2);
        fileStream.Write(BitConverter.GetBytes(blockAlign), 0, 2);

        UInt16 bps = 16;
        Byte[] bitsPerSample = BitConverter.GetBytes(bps);
        fileStream.Write(bitsPerSample, 0, 2);

        Byte[] datastring = System.Text.Encoding.UTF8.GetBytes("data");
        fileStream.Write(datastring, 0, 4);

        Byte[] subChunk2 = BitConverter.GetBytes(samples * channels * 2);
        fileStream.Write(subChunk2, 0, 4);
    }

    public static string TestFunc()
    {
        var methods = typeof(UnityEngine.Object).GetMethods(BindingFlags.Static | BindingFlags.Public);
        var method = methods.First(m => m.Name == "FindObjectsOfType" && m.GetParameters().Length == 0).MakeGenericMethod(typeof(UnityEngine.SpriteRenderer));
        object returned = method.Invoke(null, new object[0]);
        Type type = returned.GetType();
        var indexer = type.GetMethod("get_Item");
        UnityEngine.GameObject obj = (indexer.Invoke(returned, new object[] { 0 }) as Component).gameObject;

        return obj.name;
    }

    public static bool IsIndexerPropertyMethod(this MethodInfo method)
    {
        var declaringType = method.DeclaringType;
        if (declaringType is null) return false;
        var indexerProperty = GetIndexerProperty(method.DeclaringType);
        if (indexerProperty is null) return false;
        return method == indexerProperty.GetMethod || method == indexerProperty.SetMethod;
    }

    private static PropertyInfo GetIndexerProperty(this Type type)
    {
        var defaultPropertyAttribute = type.GetCustomAttributes<DefaultMemberAttribute>()
            .FirstOrDefault();
        if (defaultPropertyAttribute is null) return null;
        return type.GetProperty(defaultPropertyAttribute.MemberName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    }
}
