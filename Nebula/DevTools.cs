using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using HarmonyLib;
using BepInEx.IL2CPP;
using UnityEngine;
using Hazel;

namespace Nebula
{
    public static class DevTools
    {
        private static FileStream CreateEmptyWav(string filepath)
        {
            var fileStream = new FileStream(filepath, FileMode.Create);
            byte emptyByte = new byte();

            for (int i = 0; i < 44; i++) //preparing the header
            {
                fileStream.WriteByte(emptyByte);
            }

            return fileStream;
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
			var obj=UnityEngine.GameObject.Find(objName);
			var renderer = obj.GetComponent<SpriteRenderer>();
			if (renderer == null) return;

			byte[] bytes = UnityEngine.ImageConversion.EncodeToPNG(Helpers.CreateReadabeTexture(renderer.sprite.texture));
			//保存
			File.WriteAllBytes(fileName + ".png", bytes);
		}

		public static void SearchAndSaveTextureFromSprite(string objName, string fileName)
		{
			foreach(var sprite in UnityEngine.Object.FindObjectsOfTypeAll(Sprite.Il2CppType))
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
            foreach (var audio in UnityEngine.Object.FindObjectsOfType<AudioClip>())
            {
                var filepath = directory + "/" + audio.name + ".wav";

                // Make sure directory exists if user is saving to sub dir.
                Directory.CreateDirectory(Path.GetDirectoryName(filepath));

                using (var fileStream = CreateEmptyWav(filepath))
                {

                    ConvertAndWrite(fileStream, audio);

                    WriteHeader(fileStream, audio);
                }

            }
        }

		private static void ConvertAndWrite(FileStream fileStream, AudioClip clip)
		{

			var samples = new float[clip.samples];

			clip.GetData(samples, 0);

			Int16[] intData = new Int16[samples.Length];
			//converting in 2 float[] steps to Int16[], //then Int16[] to Byte[]

			Byte[] bytesData = new Byte[samples.Length * 2];
			//bytesData array is twice the size of
			//dataSource array because a float converted in Int16 is 2 bytes.

			int rescaleFactor = 32767; //to convert float to Int16

			for (int i = 0; i < samples.Length; i++)
			{
				intData[i] = (short)(samples[i] * rescaleFactor);
				Byte[] byteArr = new Byte[2];
				byteArr = BitConverter.GetBytes(intData[i]);
				byteArr.CopyTo(bytesData, i * 2);
			}

			fileStream.Write(bytesData, 0, bytesData.Length);
		}

		private static void WriteHeader(FileStream fileStream, AudioClip clip)
		{

			var hz = clip.frequency;
			var channels = clip.channels;
			var samples = clip.samples;

			fileStream.Seek(0, SeekOrigin.Begin);

			Byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
			fileStream.Write(riff, 0, 4);

			Byte[] chunkSize = BitConverter.GetBytes(fileStream.Length - 8);
			fileStream.Write(chunkSize, 0, 4);

			Byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
			fileStream.Write(wave, 0, 4);

			Byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
			fileStream.Write(fmt, 0, 4);

			Byte[] subChunk1 = BitConverter.GetBytes(16);
			fileStream.Write(subChunk1, 0, 4);

			UInt16 two = 2;
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

			//		fileStream.Close();
		}
	}
}
