using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utility
{
    public class PriorityQueue<T> where T : IComparable<T>
    {
        private List<T> heap = new List<T>();
        
        public int Count => heap.Count;

        // 添加元素并保持堆的性质
        public void Enqueue(T item)
        {
            heap.Add(item);
            int i = heap.Count - 1;
            while (i > 0)
            {
                int parent = (i - 1) / 2;
                if (heap[parent].CompareTo(heap[i]) <= 0)
                {
                    break; // 父节点不大于当前节点，堆性质满足
                }
                // 父节点大于当前节点，交换两节点
                (heap[parent], heap[i]) = (heap[i], heap[parent]);
                i = parent;
            }
        }
        
        // 取出并移除最小的元素
        public T Dequeue()
        {
            if (heap.Count == 0)
            {
                throw new InvalidOperationException("Priority queue is empty");
            }
            
            T minItem = heap[0];
            // 将最后一个元素放到堆顶
            heap[0] = heap[heap.Count - 1];
            heap.RemoveAt(heap.Count - 1);
            
            int i = 0;
            // 维持堆的性质，向下调整堆

            while (true)
            {
                int leftChild = 2 * i + 1;
                int rightChild = 2 * i + 2;
                int smallest = i;

                if (leftChild < heap.Count && heap[leftChild].CompareTo(heap[smallest]) < 0)
                {
                    smallest = leftChild;
                }

                if (rightChild < heap.Count && heap[rightChild].CompareTo(heap[smallest]) < 0)
                {
                    smallest = rightChild;
                }

                if (smallest == i)
                {
                    break; // 已经满足堆性质
                }

                // 交换父节点和子节点
                (heap[i], heap[smallest]) = (heap[smallest], heap[i]);
                i = smallest;
            }
            
            return minItem;
        }
        
        // 获取最小的元素
        public T Peek()
        {
            if (heap.Count == 0)
            {
                throw new InvalidOperationException("Priority queue is empty");
            }
            return heap[0];
        }
        
        // 清空队列
        public void Clear()
        {
            heap.Clear();
        }
        
        // 打印队列
        public void Print()
        {
            foreach (var item in heap)
            {
                Debug.Log(item);
            }
        }
    }
}