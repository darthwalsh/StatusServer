using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace StatusServer
{
	interface IStack<T> : IEnumerable<T>
	{
		IStack<T> Push(T value);
		IStack<T> Pop();
		T Peek();
		bool IsEmpty { get; }
	}

	sealed class ImmutableStack<T> : IStack<T>
	{
		static readonly EmptyStack empty = new EmptyStack();
		public static IStack<T> Empty { get { return empty; } }

		readonly T head;
		readonly IStack<T> tail;

		ImmutableStack(T head, IStack<T> tail) {
			this.head = head;
			this.tail = tail;
		}

		public static IStack<T> New(IEnumerable<T> range) {
			var current = Empty;
			foreach (var t in range)
				current = current.Push(t);

			return current;
		}

		public T Peek() {
			return this.head;
		}

		public IStack<T> Pop() {
			return this.tail;
		}

		public IStack<T> Push(T value) {
			return new ImmutableStack<T>(value, this);
		}

		public bool IsEmpty { get { return false; } }

		public IEnumerator<T> GetEnumerator() {
			for (IStack<T> stack = this; !stack.IsEmpty; stack = stack.Pop())
				yield return stack.Peek();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		sealed class EmptyStack : IStack<T>
		{
			public bool IsEmpty { get { return true; } }
			public T Peek() { throw new Exception("Empty stack"); }
			public IStack<T> Push(T value) { return new ImmutableStack<T>(value, this); }
			public IStack<T> Pop() { throw new Exception("Empty stack"); }
			public IEnumerator<T> GetEnumerator() { yield break; }
			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return this.GetEnumerator(); }
		}
	}
}
