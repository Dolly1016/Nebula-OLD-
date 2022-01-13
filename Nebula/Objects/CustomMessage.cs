using UnityEngine;
using System.Collections.Generic;
using System;

namespace Nebula.Objects
{

    public class CustomMessage
    {

        private TMPro.TMP_Text text;
        private static List<CustomMessage> customMessages = new List<CustomMessage>();

        public Vector3? velocity;
        //テキストをランダムに入れ替える間隔 0より大きい場合に入れ替える
        public float textSwapDuration;
        //テキストの入れ替え量
        public int textSwapGain;
        //次のテキスト入れ替え時間
        private float textSwapLeft;

        //フォントサイズの変化速度
        public Vector3? textSizeVelocity;

        public static void Initialize()
        {
            foreach(CustomMessage message in customMessages)
            {
                UnityEngine.Object.Destroy(message.text.gameObject);
            }
            customMessages.Clear();
        }

        private static void UpdateTextAlpha(ref TMPro.TMP_Text text, float phase, float fadeIn, float fadeOut)
        {
            if (fadeIn > phase)
            {
                text.color=text.color.SetAlpha(text.color.a*phase / fadeIn);
            }
            else if (1f-fadeOut < phase)
            {
                text.color = text.color.SetAlpha(text.color.a * (1f - phase) / fadeOut);
            }
        }

        //指定位置に現れる
        public static CustomMessage Create(Vector3 position,bool stickingMap,string message, float duration, float fadeInDuration, float fadeOutDuration, float fontSizeRate,  Color color1, Color? color2 = null)
        {
            return new CustomMessage(position, stickingMap, message, duration, fadeInDuration, fadeOutDuration, fontSizeRate, color1, color2);
        }

        //ランダムな位置に現れて動く
        public static CustomMessage Create(float minRadius,float maxRadius,float xRate, Vector3? velocity,string message, float duration, float fadeInDuration, float fadeOutDuration, float fontSizeRate,Color color1, Color? color2 = null)
        {
            double radius = minRadius+NebulaPlugin.rnd.NextDouble()*(maxRadius-minRadius);
            double angle = NebulaPlugin.rnd.NextDouble() * Math.PI * 2f;
            CustomMessage result= new CustomMessage(new Vector3((float)(xRate * Math.Cos(angle) * radius), (float)(Math.Sin(angle) * radius)),
                false,message, duration, fadeInDuration, fadeOutDuration, 1f, color1, color2);
            result.velocity = velocity;
            return result;
        }

        //下方に現れる
        public static CustomMessage Create(string message, float duration, float fadeInDuration, float fadeOutDuration, Color color1, Color? color2 = null)
        {
            return new CustomMessage(new Vector3(0, -1.8f, 0), false,message, duration, fadeInDuration, fadeOutDuration, 1f,color1, color2);
        }

        public CustomMessage(Vector3 position,bool stickingMap,string message, float duration, float fadeInDuration, float fadeOutDuration, float fontSizeRate,Color color1 ,Color? color2 =null)
        {
            velocity = null;
            textSwapDuration = 0;
            textSwapGain = 0;
            textSwapLeft = 0;
            textSizeVelocity = null;

            RoomTracker roomTracker = HudManager.Instance?.roomTracker;
            if (roomTracker != null)
            {
                GameObject gameObject = UnityEngine.Object.Instantiate(roomTracker.gameObject);

                gameObject.transform.SetParent(HudManager.Instance.transform);
                UnityEngine.Object.DestroyImmediate(gameObject.GetComponent<RoomTracker>());
                text = gameObject.GetComponent<TMPro.TMP_Text>();

                String originalText= Language.Language.GetString("message." + message);
                String currentText=originalText;
                text.text = originalText;

                text.transform.localScale *= fontSizeRate;
                

                if (stickingMap)
                {
                    gameObject.transform.position = position;
                }
                else
                {
                    //localPositionはマップの座標に左右されない
                    gameObject.transform.localPosition = new Vector3(0f, 0f, gameObject.transform.localPosition.z) + position;
                }

                customMessages.Add(this);

                float sum = fadeInDuration + duration + fadeOutDuration;
                HudManager.Instance.StartCoroutine(Effects.Lerp(sum, new Action<float>((p) =>
                {
                    if (velocity != null)
                    {
                        if (stickingMap)
                        {
                            gameObject.transform.position += (Vector3)velocity * Time.deltaTime;
                        }
                        else
                        {
                            gameObject.transform.localPosition += (Vector3)velocity * Time.deltaTime;
                        }
                    }
                    if (text != null)
                    {
                        //テキストのランダム入れ替え
                        if (textSwapDuration > 0f && textSwapGain > 0)
                        {
                            textSwapLeft -= Time.deltaTime;
                            if (textSwapLeft < 0)
                            {
                                char[] charArray=originalText.ToCharArray();
                                char temp;
                                int rnd1, rnd2;

                                //入れ替え
                                for(int i=0; i < textSwapGain; i++)
                                {
                                    rnd1 = NebulaPlugin.rnd.Next(charArray.Length);
                                    rnd2 = NebulaPlugin.rnd.Next(charArray.Length);

                                    temp = charArray[rnd1];
                                    charArray[rnd1] = charArray[rnd2];
                                    charArray[rnd2] = temp;
                                }
                                currentText = new string(charArray);

                                textSwapLeft = textSwapDuration;
                            }
                        }

                        if (color2 != null)
                        {
                            bool even = ((int)(p * duration / 0.25f)) % 2 == 0; // Bool flips every 0.25 seconds
                            text.color = even ? color1 : (Color)color2;
                        }
                        else
                        {
                            text.color = color1;
                        }

                        if (textSizeVelocity != null) {
                            text.transform.localScale += (Vector3)textSizeVelocity * Time.deltaTime;
                        }

                        UpdateTextAlpha(ref text, p, fadeInDuration / sum, fadeOutDuration / sum);
                        text.text = Helpers.cs(text.color, currentText);

                    }

                    //消去する
                    if (p == 1f && text != null && text.gameObject != null)
                    {
                        UnityEngine.Object.Destroy(text.gameObject);
                        customMessages.Remove(this);
                    }
                })));
            }
        }
    }
}