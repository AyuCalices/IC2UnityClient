using System.Collections;

namespace TransitionView
{
    public interface ICustomTransitionView
    {
        public void Enable();
        public IEnumerator EnableCoroutine();
        public void Disable(DeactivationType deactivationType);
        public IEnumerator DisableCoroutine(DeactivationType deactivationType);
    }
}
