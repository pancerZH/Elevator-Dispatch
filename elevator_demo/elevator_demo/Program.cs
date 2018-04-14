using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace elevator_demo
{
    public enum Status : byte{pause, up, down}; //电梯状态
    public enum Order : byte{up, down, free}; //电梯当前指令
    public enum Job : byte{up, down, free}; //电梯当前任务

    public class Elevator
    {
        public Status state = Status.pause;
        public Order order = Order.free;
        public Job job = Job.free;
        //电梯内按键
        public Dictionary<int, bool> destination = new Dictionary<int, bool>();
        public int currentFloor = 1; //当前楼层，初始为1楼
        public int no; //电梯编号

        public Elevator()
        {
            for(int i=1; i<=20; ++i)
            {
                //初始化电梯状态：所有按钮都未按下
                destination.Add(i, false);
            }
        }

        public void Runner()
        {
            while(true)
            {
                if (state == Status.up) //电梯上升中
                {
                    for (int i = currentFloor + 1; i <= 20; ++i)
                    {
                        if (destination[i]) //停靠
                        {
                            Console.WriteLine("elevator{0} stops at {1}", no, i);
                            state = Status.pause;
                            Console.WriteLine("elevator{0} opens its door", no);
                            destination[i] = false; //取消按键
                            currentFloor = i; //更新楼层
                            Thread.Sleep(500); //停靠0.5秒
                            break;
                        }
                        else
                        {
                            currentFloor = i; //更新电梯楼层
                            Console.WriteLine("elevator{0} arrives at {1}", no, i);
                            Thread.Sleep(500); //向下一层前进，用时0.5秒

                            int j;
                            for(j = currentFloor + 2; j <= 20; ++j) //检查是否还有后续楼层
                            {
                                if (destination[j])
                                    break;
                            }
                            if(j == 21) //没有后续楼层
                            {
                                state = Status.pause;
                                order = Order.free;
                                currentFloor++;
                                Console.WriteLine("elevator{0} arrives at {1}", no, currentFloor);
                                break;
                            }
                        }
                    }
                }

                else if (state == Status.down) //电梯下降中
                {
                    for (int i = currentFloor - 1; i >= 1; --i)
                    {
                        if (destination[i]) //停靠
                        {
                            Console.WriteLine("elevator{0} stops at {1}", no, i);
                            state = Status.pause;
                            Console.WriteLine("elevator{0} opens its door", no);
                            destination[i] = false; //取消按键
                            currentFloor = i;
                            Thread.Sleep(500);
                            break;
                        }
                        else
                        {
                            currentFloor = i; //更新电梯楼层
                            Console.WriteLine("elevator{0} arrives at {1}", no, i);
                            Thread.Sleep(500); //向下一层前进，用时0.5秒

                            int j;
                            for (j = currentFloor - 2; j >= 1; --j) //检查是否还有后续楼层
                            {
                                if (destination[j])
                                    break;
                            }
                            if (j == 0) //没有后续楼层
                            {
                                state = Status.pause;
                                order = Order.free;
                                currentFloor--;
                                Console.WriteLine("elevator{0} arrives at {1}", no, currentFloor);
                                break;
                            }
                        }
                    }
                }

                else //电梯处于停靠状态
                {
                    if (order == Order.up) //仍有上升指令
                    {
                        for (int i = currentFloor + 1; i <= 20; ++i)
                        {
                            if (destination[i])
                            {
                                state = Status.up;
                                break;
                            }
                        }
                        if (state == Status.pause) //已经没有上升的楼层
                        {
                            order = Order.free; //电梯没有指令
                        }
                    }
                    else if (order == Order.down) //仍有下降指令
                    {
                        for (int i = currentFloor - 1; i >= 1; --i)
                        {
                            if (destination[i])
                            {
                                state = Status.down;
                                break;
                            }
                        }
                        if (state == Status.pause) //已没有下降的楼层
                        {
                            order = Order.free; //电梯没有指令
                        }
                    }
                    else //当前没有指令
                    {
                        for(int i=1; i<=20; ++i)
                        {
                            if(destination[i])
                            {
                                if (i < currentFloor)
                                {
                                    order = Order.down;
                                    if (job == Job.free)
                                        job = Job.down;
                                }
                                else if (i > currentFloor)
                                {
                                    order = Order.up;
                                    if (job == Job.free)
                                        job = Job.up;
                                }
                                else //事实上这段代码应该永远无法执行
                                {
                                    Console.WriteLine("elevator{0} opens its door", no);
                                    destination[i] = false; //取消按键
                                    Thread.Sleep(500); //停靠0.5秒
                                }
                            }
                        }
                        if(order == Order.free) //没有指令
                        {
                            job = Job.free; //电梯没有任务
                            break;
                        }
                    }
                }
            }
        }

        public void Arrange(int floor) //点亮和取消
        {
            destination[floor] = !destination[floor];
        }

        public void Display()
        {
            for(int i=1; i<=20; ++i)
                Console.Write("{0} ", destination[i]);
            Console.WriteLine();
        }
    }

    public class ElevatorGroup
    {
        public Elevator[] elevator = new Elevator[5]; //添加5部电梯
        public ElevatorGroup()
        {
            for(int i=0; i<5; ++i)
            {
                elevator[i] = new Elevator();
                elevator[i].no = i;
            }
        }

        public void BuildThread(int no)
        {
            ThreadStart readyElevator = new ThreadStart(elevator[no].Runner);
            Thread elevatorThread = new Thread(readyElevator);
            elevatorThread.Start();
        }

        public void Dispatch(int elevatorNo, int insideFloor) //在某电梯内部选择楼层
        {
            elevator[elevatorNo].Arrange(insideFloor);
            if (elevator[elevatorNo].destination[insideFloor] && elevator[elevatorNo].order == Order.free)
            {
                if (elevator[elevatorNo].currentFloor > insideFloor) //电梯应下降
                {
                    elevator[elevatorNo].order = Order.down;
                    if (elevator[elevatorNo].job == Job.free) //安排任务
                        elevator[elevatorNo].job = Job.down;
                }
                else if (elevator[elevatorNo].currentFloor < insideFloor) //电梯应上升
                {
                    elevator[elevatorNo].order = Order.up;
                    if (elevator[elevatorNo].job == Job.free) //安排任务
                        elevator[elevatorNo].job = Job.up;
                }
                else //电梯应开门
                {
                    Console.WriteLine("elevator{0} opens its door", elevatorNo);
                    Thread.Sleep(500); //停靠0.5秒
                    elevator[elevatorNo].Arrange(insideFloor);
                    return;
                }
                BuildThread(elevatorNo);
            }
        }

        public void Dispatch(int destinationFloor, bool goUp) //在电梯外部按动向上或向下，此函数可能造成阻塞
        {
            int elevatorWhoRespect = -1;
            while(elevatorWhoRespect == -1) //防止没有合适的电梯，反复尝试直到成功
            {
                if (goUp) //上楼
                {
                    int distance = 20;
                    for (int i = 0; i < 5; ++i)
                    {
                        if (elevator[i].job == Job.up) //若电梯有上升任务
                        {
                            int currentDistance = destinationFloor - elevator[i].currentFloor;
                            if (currentDistance > 0 && currentDistance < distance)
                            {
                                elevatorWhoRespect = i;
                                distance = currentDistance;
                            }
                            else if(currentDistance == 0 && elevator[i].state == Status.pause) //恰好赶上电梯
                            {
                                Console.WriteLine("elevator{0} opens its door, lucky", i);
                                Thread.Sleep(500); //停靠0.5秒
                                return;
                            }
                        }
                        else if (elevator[i].order == Order.free) //若电梯处于空闲状态
                        {
                            int currentDistanceAbs = Math.Abs(destinationFloor - elevator[i].currentFloor);
                            if (currentDistanceAbs < distance)
                            {
                                elevatorWhoRespect = i;
                                distance = currentDistanceAbs;
                            }
                        }
                    }
                }
                else //下楼
                {
                    int distance = 20;
                    for(int i=0; i<5; ++i)
                    {
                        if(elevator[i].job == Job.down) //若电梯有下降任务
                        {
                            int currentDistance = elevator[i].currentFloor - destinationFloor;
                            if (currentDistance > 0 && currentDistance < distance)
                            {
                                elevatorWhoRespect = i;
                                distance = currentDistance;
                            }
                            else if (currentDistance == 0 && elevator[i].state == Status.pause) //恰好赶上电梯
                            {
                                Console.WriteLine("elevator{0}opens its door, lucky", i);
                                Thread.Sleep(500); //停靠0.5秒
                                return;
                            }
                        }
                        else if (elevator[i].order == Order.free) //若电梯处于空闲状态
                        {
                            int currentDistanceAbs = Math.Abs(destinationFloor - elevator[i].currentFloor);
                            if (currentDistanceAbs < distance)
                            {
                                elevatorWhoRespect = i;
                                distance = currentDistanceAbs;
                            }
                        }
                    }
                }

                if (elevatorWhoRespect == -1) //未找到合适的电梯
                    Thread.Sleep(500);
            }
            if (goUp)
                elevator[elevatorWhoRespect].job = Job.up;
            else
                elevator[elevatorWhoRespect].job = Job.down;
            Dispatch(elevatorWhoRespect, destinationFloor);
        }
    }

    class Program
    {   
        static void Main(string[] args)
        {
            ElevatorGroup elevatorGroup = new ElevatorGroup();
            string control;

            while (true)
            {
                control = Console.ReadLine();
                switch (control)
                {
                    case "1":
                        /*测试1：电梯内部的复杂按键情况*/
                        elevatorGroup.Dispatch(2, 5);
                        elevatorGroup.Dispatch(2, 12);
                        elevatorGroup.Dispatch(2, 20);
                        elevatorGroup.Dispatch(2, 15);
                        while (elevatorGroup.elevator[2].currentFloor != 13) //到达13楼后执行
                            Thread.Sleep(200);
                        elevatorGroup.Dispatch(2, 7);
                        while (elevatorGroup.elevator[2].currentFloor != 7) //到达7楼后执行
                            Thread.Sleep(200);
                        elevatorGroup.Dispatch(2, 5);
                        while (elevatorGroup.elevator[2].currentFloor != 5)
                            Thread.Sleep(200);
                        Thread.Sleep(500);
                        elevatorGroup.Dispatch(2, 5);
                        elevatorGroup.Dispatch(2, 9);
                        Console.ReadKey();
                        Console.Clear();
                        break;

                    case "2":
                        /*测试2：电梯外部的复杂按键情况*/
                        for(int i=2; i<=7; ++i)
                        {
                            elevatorGroup.Dispatch(i, false);
                            elevatorGroup.Dispatch(i, true);
                        }
                        Console.ReadKey();
                        Console.Clear();
                        break;

                    case "3":
                        /*测试3：取消按键功能（电梯内部）*/
                        elevatorGroup.Dispatch(2, 15); //要去15楼
                        while (elevatorGroup.elevator[2].currentFloor != 6) //到达6楼
                            Thread.Sleep(200);
                        elevatorGroup.Dispatch(2, 15); //取消按键
                        Thread.Sleep(1000); //犹豫1秒
                        elevatorGroup.Dispatch(2, 1); //回1楼
                        Console.ReadKey();
                        Console.Clear();
                        break;

                    case "4":
                        /*测试4：不同方向任务混合*/
                        elevatorGroup.Dispatch(1, true); //1楼向上
                        elevatorGroup.Dispatch(0, 20); //0号电梯要去20楼
                        Thread.Sleep(500);
                        elevatorGroup.Dispatch(6, true); //6楼向上，0号电梯应停靠
                        elevatorGroup.Dispatch(9, false); //9楼向下，0号电梯不应停靠
                        elevatorGroup.Dispatch(1, 1);
                        while (elevatorGroup.elevator[0].currentFloor != 15) //到达15楼
                            Thread.Sleep(200);
                        elevatorGroup.Dispatch(0, 20); //0号电梯取消20楼
                        Thread.Sleep(1000);
                        elevatorGroup.Dispatch(0, 2); //0号电梯去2楼
                        while (elevatorGroup.elevator[0].currentFloor != 14) //到达14楼
                            Thread.Sleep(200);
                        elevatorGroup.Dispatch(13, false); //13楼向下，0号电梯应停靠
                        Console.ReadKey();
                        Console.Clear();
                        break;

                    default:
                        return;
                }
            }
        }
    }
}
