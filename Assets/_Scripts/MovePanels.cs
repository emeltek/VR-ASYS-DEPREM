using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MovePanels : MonoBehaviour
{
   public List<RectTransform> questionPanels;
   

   private void Start()
   {

   }

   private void Awake()
   {
     
   }

   public void GetOtherPanel(RectTransform panel)
   {
      for (int i = 0; i < questionPanels.Count; i++)
      {
         if (panel == questionPanels[i])
         {
            questionPanels[i + 1].DOMoveY(questionPanels[i].position.y, 1.5f);
            questionPanels[i].DOMoveY(-questionPanels[i + 1].position.y, 1.5f);
         }
      }
   }
}

