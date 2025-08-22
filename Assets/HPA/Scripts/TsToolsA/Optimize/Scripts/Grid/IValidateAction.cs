// Interface used by SpawnSystem and GridOptimisation
namespace HP.Generics
{
    public interface IValidateAction<T>
    {
        public void ValidateAction(T actionState);
    }
}
