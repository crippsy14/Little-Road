using System;
using UnityEngine;
using UnityEngine.UI;

namespace LittleRoad
{
    // Older menu helpers, now thin wrappers over the Toy kit so the remaining
    // screens built with them (paddock inspector) share the toy style.
    public static class CoastMenu
    {
        public static readonly Color Cream=Toy.Cream,Pale=new(.89f,.88f,.77f),Navy=Toy.Navy;
        public static Button Button(Transform parent,string name,string caption,Vector2 size,Vector2 position,Action clicked,bool primary=false)
            =>Toy.Candy(parent,name,caption,null,size,position,primary?Toy.Gold:Toy.Blue,clicked);
        public static RectTransform Paper(Transform parent,string name,Vector2 size,Vector2 position)
            =>Toy.Card(parent,name,size,position,Toy.Cream,34,7,10).rectTransform;
        public static void RefreshButtons(Transform root)=>Toy.RefreshButtons(root);
    }
}
