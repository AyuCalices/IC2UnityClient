﻿using UnityEngine;

namespace Plugins.UnitySingleton.Scripts.Samples
{

    public class Test : MonoBehaviour
    {

        void Start()
        {
            Debug.Log(GameManager.Instance.GetPlayerName());
        }

    }

}