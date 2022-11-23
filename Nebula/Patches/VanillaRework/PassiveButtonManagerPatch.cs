namespace Nebula.Patches.VanillaRework;

[HarmonyPatch]
class PassiveButtonManagerPatch
{
    static public Vector2 ConvertToPosition(Vector2 mainCameraPosition, Camera camera, bool correctOrigin)
    {
        if (camera == Camera.main) return mainCameraPosition;
        if (correctOrigin) mainCameraPosition -= (Vector2)Camera.main.transform.position;
        Vector2 result = (mainCameraPosition / Camera.main.orthographicSize * camera.orthographicSize);
        if (correctOrigin) result += (Vector2)Camera.main.transform.position;
        return result;
    }

    static public float ConvertToScalar(float scalar, Camera camera)
    {
        if (camera == Camera.main) return scalar;
        return scalar * Camera.main.orthographicSize / camera.orthographicSize;
    }

    static public DragState CheckDrag(Controller controller, Collider2D coll, Camera camera)
    {
        if (!coll)
        {
            return DragState.NoTouch;
        }
        if (controller.touchId > -1 && (!controller.amTouching || !controller.amTouching.isActiveAndEnabled))
        {
            controller.touchId = -1;
            controller.amTouching = null;
        }
        if (controller.touchId <= -1)
        {
            int i = 0;
            foreach (var touchState in controller.Touches)
            {
                if (touchState.TouchStart && coll.OverlapPoint(ConvertToPosition(touchState.Position, camera, true)))
                {
                    controller.amTouching = coll;
                    controller.touchId = i;
                    touchState.dragState = DragState.TouchStart;
                    return DragState.TouchStart;
                }
                i++;
            }
            return DragState.NoTouch;
        }
        if (coll != controller.amTouching)
        {
            return DragState.NoTouch;
        }
        Controller.TouchState touchState2 = controller.Touches[controller.touchId];
        if (!touchState2.IsDown)
        {
            controller.amTouching = null;
            controller.touchId = -1;
            touchState2.dragState = DragState.Released;
            return DragState.Released;
        }
        if (ConvertToScalar(Vector2.Distance(touchState2.ScreenDownAt, touchState2.ScreenPosition), camera) > 10f || touchState2.dragState == DragState.Dragging)
        {
            touchState2.dragState = DragState.Dragging;
            return DragState.Dragging;
        }
        touchState2.dragState = DragState.Holding;
        return DragState.Holding;
    }

    [HarmonyPatch(typeof(PassiveButtonManager), nameof(PassiveButtonManager.Update))]
    class UpdatePatch
    {
        //Cameraに応じて座標を変換する
        static public bool Prefix(PassiveButtonManager __instance)
        {
            try
            {
                if (Camera.main.orthographicSize == 3f) return true;
            }
            catch { return true; }

            if (!Application.isFocused)
            {
                return false;
            }
            __instance.controller.Update();

            try
            {
                List<PassiveUiElement> uiElements = new List<PassiveUiElement>(__instance.Buttons.Count);
                foreach (var b in __instance.Buttons.GetFastEnumerator()) uiElements.Add(b);


                foreach (var b in uiElements)
                {
                    if (b.transform.hasChanged)
                    {
                        b.CachedZ = b.transform.position.z;
                        b.transform.hasChanged = false;
                    }
                }

                for (int j = 1; j < uiElements.Count; j++)
                {
                    if (PassiveButtonManager.DepthComparer.Instance.Compare(uiElements[j - 1], uiElements[j]) > 0)
                    {
                        uiElements.Sort((x, y) =>
                        {
                            if (x == null)
                            {
                                return 1;
                            }
                            if (y == null)
                            {
                                return -1;
                            }
                            return x.CachedZ.CompareTo(y.CachedZ);
                        });
                        break;
                    }
                }

                HandleMouseOut(__instance);

                uiElements.RemoveAll((e) => !e);

                foreach (PassiveUiElement passiveUiElement in uiElements)
                {
                    if (passiveUiElement.isActiveAndEnabled)
                    {
                        //表示に使用するカメラ
                        Camera camera = Camera.main;
                        if (passiveUiElement.gameObject.layer == LayerExpansion.GetUILayer()) camera = Camera.allCameras.FirstOrDefault((c) => c.name == "UI Camera") ?? camera;

                        if (passiveUiElement.ClickMask)
                        {
                            Controller.TouchState touch = __instance.controller.GetTouch(0);
                            Vector2 position = ConvertToPosition(touch.Position, camera, true);
                            if (touch.IsDown && !passiveUiElement.ClickMask.OverlapPoint(position))
                            {
                                continue;
                            }
                        }

                        foreach (var col in passiveUiElement.Colliders)
                        {
                            if (col && col.isActiveAndEnabled)
                            {
                                HandleMouseOver(__instance, passiveUiElement, col, camera);

                                switch (CheckDrag(__instance.controller, col, camera))
                                {
                                    case DragState.TouchStart:
                                        if (passiveUiElement.HandleDown)
                                        {
                                            passiveUiElement.ReceiveClickDown();
                                        }
                                        break;
                                    case DragState.Holding:
                                        if (passiveUiElement.HandleRepeat)
                                        {
                                            passiveUiElement.ReceiveRepeatDown();
                                        }
                                        break;
                                    case DragState.Dragging:
                                        if (passiveUiElement.HandleDrag)
                                        {
                                            Vector2 dragDelta = ConvertToPosition(__instance.controller.DragPosition - __instance.controller.DragStartPosition, camera, false);
                                            passiveUiElement.ReceiveClickDrag(dragDelta);
                                            __instance.controller.ResetDragPosition();
                                        }
                                        else if (passiveUiElement.HandleRepeat)
                                        {
                                            passiveUiElement.ReceiveRepeatDown();
                                        }
                                        else
                                        {
                                            foreach (var b in __instance.Buttons.GetFastEnumerator())
                                            {
                                                if (b.HandleDrag && b.isActiveAndEnabled && b.transform.position.z > col.transform.position.z)
                                                {
                                                    __instance.controller.ClearTouch();
                                                    break;
                                                }
                                            }

                                        }
                                        break;
                                    case DragState.Released:
                                        if (passiveUiElement.HandleUp)
                                        {
                                            passiveUiElement.ReceiveClickUp();
                                        }
                                        break;
                                }
                            }
                        }
                    }
                }
                __instance.Buttons = new Il2CppSystem.Collections.Generic.List<PassiveUiElement>(uiElements.Count);
                foreach (var e in uiElements) __instance.Buttons.Add(e);

                if (__instance.controller.AnyTouchDown)
                {
                    Vector2 touch2 = __instance.GetTouch(true);
                    HandleFocus(__instance, touch2);
                }

            }
            catch (Exception exception)
            {
                NebulaPlugin.Instance.Logger.Print(exception.ToString());
            }
            return false;
        }

        static private bool predicate(float depth, PassiveButtonManager __instance, Vector2 point)
        {
            foreach (var top in __instance.Buttons.GetFastEnumerator())
            {
                if (top.isActiveAndEnabled && top.transform.position.z < depth)
                {
                    //表示に使用するカメラ
                    Camera camera = Camera.main;
                    if (top.gameObject.layer == LayerExpansion.GetUILayer()) camera = Camera.allCameras.FirstOrDefault((c) => c.name == "UI Camera") ?? camera;

                    foreach (var c in top.Colliders)
                    {
                        if (c.OverlapPoint(ConvertToPosition(point, camera, true))) return true;
                    }
                }
            }
            return false;
        }

        //Cameraに応じて座標を変換する
        static public void HandleFocus(PassiveButtonManager __instance, Vector2 pt)
        {
            bool flag = false;
            for (int i = 0; i < __instance.FocusHolders.Count; i++)
            {
                IFocusHolder focusHolder = __instance.FocusHolders[i];
                try
                {
                    MonoBehaviour behaviour = focusHolder.CastFast<MonoBehaviour>();
                    if (focusHolder.CheckCollision(pt))
                    {
                        float depth = behaviour.transform.position.z;
                        if (!predicate(depth, __instance, pt))
                        {
                            flag = true;
                            focusHolder.GiveFocus();
                            foreach (var holder2 in __instance.FocusHolders)
                            {
                                if (focusHolder != holder2) holder2.LoseFocus();
                            }
                            break;
                        }
                        break;
                    }
                }
                catch
                {
                    __instance.FocusHolders.RemoveAt(i);
                    i--;
                }
            }
            if (!flag)
            {
                foreach (var holder in __instance.FocusHolders)
                {
                    holder.LoseFocus();
                }
            }
        }

        //Cameraに応じて座標を変換する
        static public void HandleMouseOut(PassiveButtonManager __instance)
        {
            if (__instance.currentOver)
            {
                //表示に使用するカメラ
                Camera camera = Camera.main;
                if (__instance.currentOver.gameObject.layer == LayerExpansion.GetUILayer()) camera = Camera.allCameras.FirstOrDefault((c) => c.name == "UI Camera") ?? camera;

                bool flag = false;
                int index = 0;
                foreach (var pt in __instance.controller.Touches)
                {
                    if (pt.active)
                    {
                        foreach (var c in __instance.currentOver.Colliders)
                        {
                            if (c.OverlapPoint(ConvertToPosition(pt.Position, camera, true)))
                            {
                                flag = true;
                                break;
                            }
                        }
                    }
                    index++;
                }
                if (!flag)
                {
                    __instance.currentOver.ReceiveMouseOut();
                    __instance.currentOver = null;
                }
            }
        }


        //Cameraに応じて座標を変換する
        static public void HandleMouseOver(PassiveButtonManager __instance, PassiveUiElement button, Collider2D col, Camera camera)
        {
            if (!button.HandleOverOut || button == __instance.currentOver)
            {
                return;
            }

            if (button.ClickMask)
            {
                Vector2 position = ConvertToPosition(__instance.controller.GetTouch(0).Position, camera, true);
                if (!button.ClickMask.OverlapPoint(position))
                {
                    return;
                }
            }
            if (__instance.currentOver && button.transform.position.z > __instance.currentOver.transform.position.z)
            {
                return;
            }
            bool flag = false;
            foreach (var touch in __instance.controller.Touches)
            {
                if (touch.active && col.OverlapPoint(ConvertToPosition(touch.Position, camera, true)))
                {
                    flag = true;
                    break;
                }
            }
            if (flag)
            {
                if (__instance.currentOver && __instance.currentOver != button)
                {
                    __instance.currentOver.ReceiveMouseOut();
                }
                __instance.currentOver = button;
                __instance.currentOver.ReceiveMouseOver();
                return;
            }
        }
    }
}