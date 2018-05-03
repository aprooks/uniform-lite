using System;

namespace ddd
{
    public interface IBoundedContext{}

    public interface IAggregate { }

    public interface ICommand { }

    public interface IQuery { }

    public interface ICommand<T>: ICommand where T : IAggregate { }

    public interface IQuery<T, Res> : IQuery where T : IAggregate { }

    public interface Id
    {
        string ToSystem();
    }

    public interface IClient<T> where T : IAggregate
    {
        void Tell(ICommand<T> cmd);

        Res Ask<Res>(IQuery<T, Res> qry);
    }

    public abstract class Aggregate<T> where T : IAggregate
    {
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

    /// Merge with base class?
    public abstract class Aggregate<T, TId>: Aggregate<T>
        where T : IAggregate
        where TId : Id
    {
        public TId Id { get; private set; }

        internal void Init(TId id)
        {
            Id = id;
            this.OnLoad();
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
        public class BaseAggregate<T, TId> : Aggregate<T, TId>
            where T:IAggregate
            where TId: Id
        {
            public override void OnLoad()
            {
                Console.WriteLine("BASE [{0}]: loaded\n",Id.ToSystem());
            }

            public override object Dispatch(object msg)
            {
                Console.WriteLine("BASE [{0}]: Pre dispatching: {1}", Id.ToSystem(), msg.GetType());
                var res = base.Dispatch(msg);
                Console.WriteLine("BASE [{0}]: Dispatched: {1} with result: {2}\n", Id.ToSystem(), msg.GetType(), res);

                return res;
            }
        }

        namespace Interface
        {
            public interface IItem : IAggregate { }

            public class Open : ICommand<IItem> { }

            public class Close : ICommand<IItem> { }

            public class IsClosed : IQuery<IItem, bool> { }

            public class ItemId :  Id
            {
                public ItemId(string id)
                {
                    Id = id.ToUpperInvariant();
                }

                public string Id { get; }

                public string ToSystem() => Id;
            }

            public interface IItemsRepository
            {
                int Load(ItemId id);
                void Save(ItemId id);
            }

        }

        namespace Aggregates
        {
            using ddd.Items.Interface;

            public class Item : BaseAggregate<Interface.IItem, ItemId>
            {
                int state;
                bool isClosed = false;
                private readonly IItemsRepository repo;

                public Item(IItemsRepository repo)
                {
                    this.repo = repo;
                }

                public override void OnLoad()
                {
                    this.state = 42;
                    Console.WriteLine("Loaded");
                    base.OnLoad();
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
    namespace Implementations
    {
        using ddd.Items.Interface;

        public class ItemsRepository : IItemsRepository
        {
            public int Load(ItemId id)
            {
                return 42;
            }

            public void Save(ItemId id)
            {
                throw new NotImplementedException();
            }
        }

        public class Finance : IBoundedContext
        {
            public static IClient<T> Get<T>(Id id) where T : IAggregate
            {
                //Hook with Autorfac
                if (typeof(T) == typeof(IItem))
                {
                    var item = new Items.Aggregates.Item(
                            new ItemsRepository()
                        );
                    item.Init((dynamic)id);
                    return (IClient<T>)
                        new Client<Items.Interface.IItem>(item);
                };

                throw new Exception("type is not registered");
            }
        }
    }


    class Program
    {
        static void Main(string[] args)
        {

            var item = Implementations.Finance.Get<Items.Interface.IItem>(
                    new Items.Interface.ItemId("33dd1a")
                );

            Console.WriteLine("Item isClosed: {0}", item.Ask(new Items.Interface.IsClosed()));

            item.Tell(new Items.Interface.Open());
            item.Tell(new Items.Interface.Close());

            Console.WriteLine("Item isClosed: {0}", item.Ask(new Items.Interface.IsClosed()));

            Console.WriteLine("Hello World!");
            Console.ReadLine();
        }
    }
}
