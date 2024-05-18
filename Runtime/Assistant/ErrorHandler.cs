using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OpenAI
{
    public class ErrorHandler : MonoBehaviour
    {
        // Start is called before the first frame update
        private void Start()
        {
        }

        // Update is called once per frame
        private void Update()
        {
        }
    }

    public enum ErrorType
    {
        RunFailed,
        RequestFailed,
        ElevenLabsRequestFailed,
    }
}