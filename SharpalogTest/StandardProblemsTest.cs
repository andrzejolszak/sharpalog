namespace Sharplog
{
    using NUnit.Framework;

    public class StandardProblemsTest
    {
        [Test]
        public void Ancestor()
        {
            Universe target = new Universe();
            string src = @"
% Equality test
ancestor(A, B) :-
    parent(A, B).
ancestor(A, B) :-
    parent(A, C),
    D = C,      % Unification required
    ancestor(D, B).
parent(john, douglas).
parent(bob, john).
parent(ebbon, bob).

assert: ancestor(A, B), count = 6?
assert: ancestor(bob, B), count = 2?
assert: ancestor(X, ebbon), count = 0?
";
            _ = target.ExecuteAll(src);
        }

        [Test]
        public void BidiPath()
        {
            Universe target = new Universe();
            string src = @"
% path test from Chen & Warren
edge(a, b). edge(b, c). edge(c, d). edge(d, a).
path(X, Y) :- edge(X, Y).
path(X, Y) :- edge(X, Z), path(Z, Y).
path(X, Y) :- path(X, Z), edge(Z, Y).

assert: path(X, Y), count = 16?
";
            _ = target.ExecuteAll(src);
        }

        [Test]
        public void Laps()
        {
            Universe target = new Universe();
            string src = @"
% Laps Test
contains(ca, store, rams_couch, rams).
contains(rams, fetch, rams_couch, will).
contains(ca, fetch, Name, Watcher) :-
    contains(ca, store, Name, Owner),
    contains(Owner, fetch, Name, Watcher).
trusted(ca).
permit(User, Priv, Name) :-
    contains(Auth, Priv, Name, User),
    trusted(Auth).

assert: permit(User, Priv, Name), count = 2?
";
            _ = target.ExecuteAll(src);
        }

        [Test]
        public void Path()
        {
            Universe target = new Universe();
            string src = @"
% path test from Chen & Warren
edge(a, b). edge(b, c). edge(c, d). edge(d, a).
path(X, Y) :- edge(X, Y).
path(X, Y) :- edge(X, Z), path(Z, Y).

assert: path(X, Y), count = 16?
";
            _ = target.ExecuteAll(src);
        }

        [Test]
        public void Pq()
        {
            Universe target = new Universe();
            string src = @"
% p q test from Chen & Warren
q(X) :- p(X).
q(a).
p(X) :- q(X).

assert: q(X), count = 1?
";
            _ = target.ExecuteAll(src);
        }

        [Test]
        public void RevPath()
        {
            Universe target = new Universe();
            string src = @"
% path test from Chen & Warren
edge(a, b). edge(b, c). edge(c, d). edge(d, a).
path(X, Y) :- edge(X, Y).
path(X, Y) :- path(X, Z), edge(Z, Y).

assert: path(X, Y), count = 16?
";
            _ = target.ExecuteAll(src);
        }

        [Test]
        public void Tc()
        {
            Universe target = new Universe();
            string src = @"
% Transitive closure test from Guo & Gupta

r(X, Y) :- r(X, Z), r(Z, Y).
r(X, Y) :- p(X, Y), q(Y).
p(a, b).  p(b, d).  p(b, c).
q(b).  q(c).

assert: r(a, Y), count = 2?
";
            _ = target.ExecuteAll(src);
        }
    }
}