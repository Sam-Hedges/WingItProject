namespace Player
{
    interface ICurrentState
    {
        PlayerState CurrentState();
    }

    class PlayerState : ICurrentState
    {
        public virtual PlayerState CurrentState()
        {
            while (1 == 1)
            {
                return new IdleState();
            }

            return this;
        }
    }

    #region SubStates
    class IdleState : PlayerState
    {
        public override PlayerState CurrentState()
        {
            return this;
        }
    }

    class JumpingState : PlayerState
    {
        public override PlayerState CurrentState()
        {
            return this;
        }
    }

    class MovingState : PlayerState
    {
        public override PlayerState CurrentState()
        {
            return this;
        }
    }

    #endregion
}