using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;

namespace elevator
{
    public partial class Form1 : Form
    {
        public enum Status : byte { pause, up, down }; //电梯状态
        public enum Order : byte { up, down, free }; //电梯当前指令
        public enum Job : byte { up, down, free }; //电梯当前任务
        ElevatorGroup elevatorGroup = new ElevatorGroup(); //创建电梯组对象
        public static Form1 form1;

        public static Dictionary<int, int> levelUp = new Dictionary<int, int>(); //记录向上的楼层按钮按下状态（2~20）
        public static Dictionary<int, int> levelDown = new Dictionary<int, int>(); //记录向下的楼层按钮按下状态（1~19）

        public class Elevator
        {
            public Status state = Status.pause;
            public Order order = Order.free;
            public Job job = Job.free;
            //电梯内按键
            public Dictionary<int, bool> destination = new Dictionary<int, bool>();
            public int currentFloor = 1; //当前楼层，初始为1楼
            public int no; //电梯编号
            public string labelName = "labelElevatorLevel";

            public Elevator()
            {
                for (int i = 1; i <= 20; ++i)
                {
                    //初始化电梯状态：所有按钮都未按下
                    destination.Add(i, false);
                }
            }

            public void Runner()
            {
                while (true)
                {
                    if (state == Status.up) //电梯上升中
                    {
                        for (int i = currentFloor; i <= 20; ++i)
                        {
                            if (destination[i]) //停靠
                            {
                                form1.write_To_Label(labelName, i.ToString() + "↑");
                                state = Status.pause;
                                form1.write_To_Label(labelName, i.ToString() + "(open)");
                                Thread.Sleep(500); //停靠0.5秒
                                destination[i] = false; //取消按键
                                if (levelUp[i] == no)
                                    levelUp[i] = -1; //激活按键
                                if (levelDown[i] == no)
                                    levelDown[i] = -1; //激活按键
                                currentFloor = i; //更新楼层
                                break;
                            }
                            else
                            {
                                currentFloor = i; //更新电梯楼层
                                form1.write_To_Label(labelName, i.ToString() + "↑");
                                Thread.Sleep(500); //向下一层前进，用时0.5秒

                                int j;
                                for (j = currentFloor + 2; j <= 20; ++j) //检查是否还有后续楼层
                                {
                                    if (destination[j])
                                        break;
                                }
                                if (j == 21) //没有后续楼层
                                {
                                    state = Status.pause;
                                    order = Order.free;
                                    currentFloor++;
                                    form1.write_To_Label(labelName, i.ToString());
                                    break;
                                }
                            }
                        }
                    }

                    else if (state == Status.down) //电梯下降中
                    {
                        for (int i = currentFloor; i >= 1; --i)
                        {
                            if (destination[i]) //停靠
                            {
                                form1.write_To_Label(labelName, i.ToString() + "↓");
                                state = Status.pause;
                                form1.write_To_Label(labelName, i.ToString() + "(open)");
                                Thread.Sleep(500);
                                destination[i] = false; //取消按键
                                if (levelUp[i] == no)
                                    levelUp[i] = -1; //激活按键
                                if (levelDown[i] == no)
                                    levelDown[i] = -1; //激活按键
                                currentFloor = i;
                                break;
                            }
                            else
                            {
                                currentFloor = i; //更新电梯楼层
                                form1.write_To_Label(labelName, i.ToString() + "↓");
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
                                    form1.write_To_Label(labelName, i.ToString());
                                    break;
                                }
                            }
                        }
                    }

                    else //电梯处于停靠状态
                    {
                        if (order == Order.up) //仍有上升指令
                        {
                            for (int i = currentFloor ; i <= 20; ++i)
                            {
                                if (destination[i] && i == currentFloor) 
                                {
                                    form1.write_To_Label(labelName, i.ToString() + "(open)");
                                    Thread.Sleep(500);
                                    destination[i] = false; //取消按键
                                    if (levelUp[i] == no)
                                        levelUp[i] = -1; //激活按键
                                    if (levelDown[i] == no)
                                        levelDown[i] = -1; //激活按键
                                }
                                else if (destination[i] && i != currentFloor)
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
                            for (int i = currentFloor; i >= 1; --i)
                            {
                                if (destination[i] && i == currentFloor)
                                {
                                    form1.write_To_Label(labelName, i.ToString() + "(open)");
                                    Thread.Sleep(500);
                                    destination[i] = false; //取消按键
                                    if (levelUp[i] == no)
                                        levelUp[i] = -1; //激活按键
                                    if (levelDown[i] == no)
                                        levelDown[i] = -1; //激活按键
                                }
                                else if (destination[i] && i != currentFloor)
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
                            for (int i = 1; i <= 20; ++i)
                            {
                                if (destination[i])
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
                                    else
                                    {
                                        form1.write_To_Label(labelName, i.ToString() + "(open)");
                                        Thread.Sleep(500); //停靠0.5秒
                                        destination[i] = false; //取消按键
                                        if (levelUp[i] == no)
                                            levelUp[i] = -1; //激活按键
                                        if (levelDown[i] == no)
                                            levelDown[i] = -1; //激活按键
                                    }
                                }
                            }
                            if (order == Order.free) //没有指令
                            {
                                job = Job.free; //电梯没有任务
                                form1.write_To_Label(labelName, currentFloor.ToString());
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
        }

        public class ElevatorGroup
        {
            public Elevator[] elevator = new Elevator[5]; //添加5部电梯
            public ElevatorGroup()
            {
                for (int i = 0; i < 5; ++i)
                {
                    elevator[i] = new Elevator();
                    elevator[i].no = i;
                    elevator[i].labelName += Convert.ToString(i);
                }
            }

            public void BuildThread(int no)
            {
                ThreadStart readyElevator = new ThreadStart(elevator[no].Runner);
                Thread elevatorThread = new Thread(readyElevator);
                elevatorThread.IsBackground = true;
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
                    BuildThread(elevatorNo);
                }
            }

            public bool Dispatch(int destinationFloor, bool goUp) //在电梯外部按动向上或向下，此函数可能造成阻塞
            {
                int elevatorWhoRespect = -1;
                while (elevatorWhoRespect == -1) //防止没有合适的电梯，反复尝试直到成功
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
                                else if (currentDistance == 0 && elevator[i].state == Status.pause) //恰好赶上电梯
                                {
                                    form1.write_To_Label(elevator[i].labelName, elevator[i].currentFloor.ToString() + "(open lucky)");
                                    Thread.Sleep(500); //停靠0.5秒
                                    levelUp[i] = -1; //激活按键
                                    form1.write_To_Label(elevator[i].labelName, elevator[i].currentFloor.ToString());
                                    return true;
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
                        for (int i = 0; i < 5; ++i)
                        {
                            if (elevator[i].job == Job.down) //若电梯有下降任务
                            {
                                int currentDistance = elevator[i].currentFloor - destinationFloor;
                                if (currentDistance > 0 && currentDistance < distance)
                                {
                                    elevatorWhoRespect = i;
                                    distance = currentDistance;
                                }
                                else if (currentDistance == 0 && elevator[i].state == Status.pause) //恰好赶上电梯
                                {
                                    form1.write_To_Label(elevator[i].labelName, elevator[i].currentFloor.ToString() + "(open lucky)");
                                    Thread.Sleep(500); //停靠0.5秒
                                    levelDown[i] = -1; //激活按键
                                    form1.write_To_Label(elevator[i].labelName, elevator[i].currentFloor.ToString());
                                    return true;
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
                        return false;
                }
                if (goUp)
                {
                    elevator[elevatorWhoRespect].job = Job.up;
                    levelUp[destinationFloor] = elevatorWhoRespect; //记录哪部电梯响应此请求
                }
                else
                {
                    elevator[elevatorWhoRespect].job = Job.down;
                    levelDown[destinationFloor] = elevatorWhoRespect; //记录哪部电梯响应此请求
                }
                Dispatch(elevatorWhoRespect, destinationFloor);
                return true;
            }
        }

        public Form1()
        {
            InitializeComponent();
            this.comboBoxLevel.SelectedIndex = 0; //设置下拉框的默认值
            form1 = this;
            //初始化楼层按钮状态
            for (int i = 1; i <= 20; ++i)
                levelUp.Add(i, -1);
            for (int i = 1; i <= 20; ++i)
                levelDown.Add(i, -1);
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void buttonLevel_Click(object sender, EventArgs e) //电梯内部按钮
        {
            RadioButton currentElevator;
            int currentNo = -1;
            for (int i=0; i<5; ++i)
            {
                currentElevator = panelElevator.Controls[i] as RadioButton;
                if(currentElevator.Checked) //找到当前选中的电梯
                {
                    currentNo = i;
                    break;
                }
            }

            Button button = sender as Button;
            int destinationLevel = int.Parse(button.Text); //找到目标楼层
            if(currentNo != -1) //正确选择了电梯
                elevatorGroup.Dispatch(currentNo, destinationLevel);
        }

        public void write_To_Label(string labelName, string content)
        {
            Control.CheckForIllegalCrossThreadCalls = false; //关掉对于线程同步的警告
            Label label = this.Controls.Find(labelName, true)[0] as Label;
            label.Text = content;
        }

        private void buttonUp_Click(object sender, EventArgs e)
        {
            ComboBox combox = this.Controls.Find("comboBoxLevel", true)[0] as ComboBox;
            int level = int.Parse(combox.Text);
            if (levelUp[level] < 0 && level != 20) //向上按钮未被按下且不是20楼（不能再向上）
            {
                levelUp[level] = 100; //100代表无法再被按下
                if (!elevatorGroup.Dispatch(int.Parse(combox.Text), true)) //未找到合适的电梯
                    levelUp[level] = -1;
            }
        }

        private void buttonDown_Click(object sender, EventArgs e)
        {
            ComboBox combox = this.Controls.Find("comboBoxLevel", true)[0] as ComboBox;
            int level = int.Parse(combox.Text);
            if (levelDown[level] < 0 && level != 1) //向下按钮未被按下且不是1楼（不能再向下）
            {
                levelDown[level] = 100; //100代表无法再被按下
                if (!elevatorGroup.Dispatch(int.Parse(combox.Text), false)) //未找到合适的电梯
                    levelDown[level] = -1;
            }
        }
    }
}
