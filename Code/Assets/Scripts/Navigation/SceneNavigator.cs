using UnityEngine;
using UnityEngine.SceneManagement;

namespace RoverMars.Navigation
{
    public sealed class SceneNavigator : MonoBehaviour
    {
        public bool IsLoading { get; private set; }

        public bool NavigateTo(string scenePath)
        {
            if (IsLoading)
                return false;

            if (string.IsNullOrWhiteSpace(scenePath) || !Application.CanStreamedLevelBeLoaded(scenePath))
            {
                Debug.LogError("Navigation destination is missing from the build scene list: " + scenePath, this);
                return false;
            }

            IsLoading = true;
            try
            {
                AsyncOperation operation = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Single);
                if (operation == null)
                {
                    IsLoading = false;
                    return false;
                }
                operation.completed += OnLoadCompleted;
                return true;
            }
            catch (System.Exception exception)
            {
                IsLoading = false;
                Debug.LogException(exception, this);
                return false;
            }
        }

        private void OnLoadCompleted(AsyncOperation operation)
        {
            IsLoading = false;
        }
    }
}
