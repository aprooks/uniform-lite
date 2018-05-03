using System;
using ddd.Items.Interface;

namespace ddd
{
    public interface IBoundedContext{}

    public interface IAggregate { }

    public interface ICommand { }

    public interface IQuery { }

    public interface ICommand<T>: ICommand where T : IAggregate { }

    public interface IQuery<T, Res> : IQuery where T : IAggregate { }

    public interface IClient<T> where T : IAggregate
    {
        void Tell(ICommand<T> cmd);

        Res Ask<Res>(IQuery<T, Res> qry);
    }

    public abstract class Aggregate<T> where T : IAggregate
    {
        public Aggregate()
        {
            OnLoad();
        }

        internal object Receive(object msg)
        {
            if (msg is IQuery qry)
            {
                //return this
                //   .GetType()
                //   .GetMethod("Answer", new Type[] { msg.GetType() })
                //   .Invoke(this, new object[] { msg });
                return ((dynamic)this).Answer((dynamic)qry);
            }
            if (msg is ICommand cmd)
            {
                this
                    .GetType()
                    .GetMethod("Handle", new Type[] { msg.GetType() })
                    .Invoke(this, new object[] { msg });
                return null;
            }

            throw new Exception("Unknown type");
        }

        public abstract void OnLoad();

        public virtual object Dispatch(object msg)
        {
            return Receive(msg);
        }
    }

    public class Client<T> : IClient<T> where T : IAggregate
    {
        private readonly Aggregate<T> aggregate;

        public Client(Aggregate<T> aggregate)
        {
            this.aggregate = aggregate;
        }

        public Res Ask<Res>(IQuery<T, Res> qry)
        {
            return (Res)aggregate.Dispatch(qry);
        }

        public void Tell(ICommand<T> cmd)
        {
            aggregate.Dispatch(cmd);
        }
    }
    namespace Other
    {
        namespace Interface
        {
            public interface IOther : IAggregate { }

            public class Test : ICommand<IOther> { }
        }
    }

    namespace Items
    {
        namespace Interface
        {
            public interface IItem : IAggregate { }

            public class Open : ICommand<IItem> { }

            public class Close : ICommand<IItem> { }

            public class IsClosed : IQuery<IItem, bool> { }
        }

        namespace Aggregates
        {
            public class Item : Aggregate<Interface.IItem>
            {
                int state;
                bool isClosed = false;

                public override void OnLoad()
                {
                    this.state = 42;
                    Console.WriteLine("Loaded");
                }

                public override object Dispatch(object msg)
                {
                    Console.WriteLine("On dispatch: {0}", msg.GetType());
                    var res = base.Dispatch(msg);
                    Console.WriteLine("Handled: {0}\n", msg.GetType());

                    return res;
                }


                public void Handle(Open cmd)
                {
                    state++;
                    Console.WriteLine("Handled open, state is: {0}",state);
                }

                public void Handle(Close cmd)
                {
                    Console.WriteLine("Handled close");
                    isClosed = true;
                }

                public bool Answer(IsClosed qry) => this.isClosed;
            }
        }
    }

    public class Finance : IBoundedContext
    {
        public static IClient<T> Get<T>(string id) where T:IAggregate
        {
            if (typeof(T)==typeof(IItem))
            {
                return (IClient<T>)
                    new Client<Items.Interface.IItem>( new Items.Aggregates.Item() );
            };

            throw new Exception("type is not registered");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {

            var item = Finance.Get<Items.Interface.IItem>("huy");

            Console.WriteLine("Item isClosed: {0}", item.Ask(new IsClosed()));

            item.Tell(new Open());
            item.Tell(new Close());

            Console.WriteLine("Item isClosed: {0}", item.Ask(new IsClosed()));

            Console.WriteLine("Hello World!");
        }
    }
}
