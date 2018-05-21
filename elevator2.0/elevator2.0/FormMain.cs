using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace elevator2._0
{
    public partial class FormMain : Form
    {
        public const int ELEVATOR_NUM = 5; // 常量，电梯数目
        public const int LEVEL_NUM = 20;  // 常量，楼层数目

        /*记录窗口控件对象的表格*/
        private static List<Button> levelUpButton = new List<Button>();  // 记录所有的向上的按钮
        private static List<Button> levelDownButton = new List<Button>();  // 记录所有的向下的按钮
        public static Dictionary<int, Button> elevatorButton = new Dictionary<int, Button>();  // 记录所有电梯内的按钮
        public static Dictionary<int, Label> elevatorLabel = new Dictionary<int, Label>();  // 记录电梯对应的标签
        public static Dictionary<int, Timer> timerGroup = new Dictionary<int, Timer>();  // 记录时钟对象

        /*用于调度的状态记录表格*/
        public static Dictionary<int, bool> levelUp = new Dictionary<int, bool>();  // 记录向上的楼层按钮按下状态
        public static Dictionary<int, bool> levelDown = new Dictionary<int, bool>();  // 记录向下的楼层按钮按下状态
        public static Dictionary<int, Elevator> elevatorGroup = new Dictionary<int, Elevator>();  // 记录电梯对象
        public static List<Job> requestWait = new List<Job>();  // 记录未能处理的任务
        public static Dictionary<int, int> whoDealTheUpRequest = new Dictionary<int, int>();  // 记录哪部电梯响应了哪个上升任务
        public static Dictionary<int, int> whoDealTheDownRequest = new Dictionary<int, int>();  // 记录哪部电梯响应了哪个下降任务
        public struct Job  // 外部请求任务的结构体
        {
            public int requestLevel;
            public bool wantUp;
            public bool hasFinished;
        };
        
        /*电梯状态的枚举类型*/
        public enum Status : byte { pause, up, down };  //电梯状态
        public enum Order : byte { up, down, free };  //电梯当前任务

        /*电梯对象*/
        public class Elevator  // 电梯PCB
        {
            public Status status = Status.pause;
            public Order order = Order.free;

            //电梯内按键
            public Dictionary<int, bool> destination = new Dictionary<int, bool>();
            public int currentFloor = 1; //当前楼层，初始为1楼
            public int no; //电梯编号

            public Elevator(int elevatorNo)
            {
                for(int i=1; i<=LEVEL_NUM; ++i)
                    destination.Add(i, false);

                no = elevatorNo;
            }
        }

        /*窗口初始化，并将各种控件对象装入对应表格*/
        public FormMain()
        {
            InitializeComponent();
            comboBoxLevel.SelectedIndex = 0;  // 设置下拉框的默认值
            for(int i=1; i<=LEVEL_NUM; ++i)  // 初始化电梯外部按钮状态
            {
                levelUp.Add(i, false);
                levelDown.Add(i, false);
            }
            for (int i = 1; i <= ELEVATOR_NUM; ++i)  // 实例化电梯对象
                elevatorGroup.Add(i, new Elevator(i));
            foreach (Timer timer in this.components.Components.OfType<Timer>())  // 为时钟分类
                if(int.Parse(timer.Tag.ToString()) != 0)
                    timerGroup.Add(int.Parse(timer.Tag.ToString()), timer);
            group_control(this);
        }

        public void group_control(Control item)  // 递归地为各类按钮和标签分组
        {
            for (int i = 0; i < item.Controls.Count; ++i)  // 将各类按钮和标签分组
            {
                if (item.Controls[i].HasChildren)
                    group_control(item.Controls[i]);

                // 为电梯外部上升下降按钮分类
                if(item.Controls[i].Text.Contains('↑'))
                    levelUpButton.Add(item.Controls[i] as Button);
                else if (item.Controls[i].Text.Contains('↓'))
                    levelDownButton.Add(item.Controls[i] as Button);

                // 为电梯内部按钮分类
                if (item.Controls[i].Name.Contains("buttonLevel"))
                    elevatorButton.Add(int.Parse(item.Controls[i].Text), item.Controls[i] as Button);

                // 为电梯对应标签分类
                if (item.Controls[i].Name.Contains("labelElevator"))
                    elevatorLabel.Add(item.Controls[i].TabIndex + 1, item.Controls[i] as Label);
            }
        }

        /*更新窗口中各类控件的状态*/
        public void renew_outside_button()  // 更新电梯外部按钮状态
        {
            ComboBox combox = this.Controls.Find("comboBoxLevel", true)[0] as ComboBox;
            int level = int.Parse(combox.Text);  // 找到当前楼层
            bool upEnable = true, downEnable = true;  // 代表外部按钮状态
            
            if (!levelUp.ContainsKey(level))  // 若字典中不包含键，说明还未初始化
                return;
            // 根据按下情况更新按钮可按下状态
            if (levelUp[level])
                upEnable = false;
            else if (!levelUp[level])
                upEnable = true;
            if (levelDown[level])
                downEnable = false;
            else if (!levelDown[level])
                downEnable = true;

            // 根据楼层更新按钮可按下状态
            if (level == 1)
                downEnable = false;
            else if (level == LEVEL_NUM)
                upEnable = false;

            // 更新按钮状态
            foreach (Button button in levelUpButton)
                button.Enabled = upEnable;
            foreach (Button button in levelDownButton)
                button.Enabled = downEnable;
        }

        public void renew_inside_button()  // 更新电梯内部按钮状态
        {
            RadioButton currentElevator;
            int currentNo = -1;  // 当前电梯编号
            for (int i = 0; i < ELEVATOR_NUM; ++i)
            {
                currentElevator = panelElevator.Controls[i] as RadioButton;
                if (currentElevator.Checked) //找到当前选中的电梯
                {
                    currentNo = int.Parse(currentElevator.Text);
                    break;
                }
            }

            if (!elevatorButton.ContainsKey(currentNo))  // 若字典中不包含键，说明还未初始化
                return;
            Elevator chosenElevator = elevatorGroup[currentNo];
            foreach(KeyValuePair<int, bool> kvp in chosenElevator.destination)
                elevatorButton[kvp.Key].Enabled = !kvp.Value;
        }

        public void renew_elevator_label(int currentNo)  // 更新电梯标签
        {
            Elevator currentElevator = elevatorGroup[currentNo];
            string content = currentElevator.currentFloor.ToString();
            if (currentElevator.status == Status.pause && currentElevator.order != Order.free)
                content += "(open)";
            else if (currentElevator.status == Status.up && currentElevator.order == Order.up)
                content += '↑';
            else if (currentElevator.status == Status.down && currentElevator.order == Order.down) 
                content += '↓';

            elevatorLabel[currentNo].Text = content;
        }

        /*窗口控件事件响应*/
        private void buttonLevel_Click(object sender, EventArgs e)  // 电梯内按钮按下之后调用
        {
            Button button = sender as Button;
            button.Enabled = false;
            RadioButton currentElevator;
            int currentNo = -1;  // 当前电梯编号
            for (int i = 0; i < ELEVATOR_NUM; ++i)
            {
                currentElevator = panelElevator.Controls[i] as RadioButton;
                if (currentElevator.Checked) //找到当前选中的电梯
                {
                    currentNo = int.Parse(currentElevator.Text);
                    break;
                }
            }

            button.Enabled = false;
            add_mission(elevatorGroup[currentNo], int.Parse(button.Text));
        }

        private void buttonUpDown_Click(object sender, EventArgs e)  // 电梯外部按钮按下之后调用
        {
            Button button = sender as Button;
            ComboBox combox = this.Controls.Find("comboBoxLevel", true)[0] as ComboBox;
            int level = int.Parse(combox.Text);  // 找到当前楼层

            if (button.Text.Contains('↑'))  // 向上的按钮被按下
            {
                levelUp[level] = true;
                dispatch(level, true);
            }
            else  // 向下的按钮被按下
            {
                levelDown[level] = true;
                dispatch(level, false);
            }

            renew_outside_button();
        }

        private void radioButtonElevator_Click(object sender, EventArgs e)  // 切换电梯按钮按下之后调用
        {
            renew_inside_button();
        }

        private void comboBoxLevel_SelectedIndexChanged(object sender, EventArgs e)  // 切换楼层之后调用
        {
            renew_outside_button();
        }

        private void timerElevator_Tick(object sender, EventArgs e)  // 时钟控件，每间隔1秒运行一次
        {
            // 获取当前电梯
            Timer timer = sender as Timer;
            int currentNo = int.Parse(timer.Tag.ToString());
            Elevator currentElevator = elevatorGroup[currentNo];

            // 更新楼层信息
            if (currentElevator.status == Status.up)
                currentElevator.currentFloor++;
            else if (currentElevator.status == Status.down)
                currentElevator.currentFloor--;

            // 进行状态转移
            if (currentElevator.status == Status.pause)
            {
                check_go_ahead(currentElevator);
                renew_elevator_label(currentNo);
            }
            else
            {
                check_pause(currentElevator);
                renew_elevator_label(currentNo);
            }

            // 更新按钮状态
            renew_inside_button();
        }

        private void timerExtraDispatch_Tick(object sender, EventArgs e)  // 时钟控件，检查等待队列，尝试为等待队列中的任务分配电梯，每隔0.5秒运行一次
        {
            for (int i = 0; i < requestWait.Count(); ++i)
                dispatch(requestWait[i].requestLevel, requestWait[i].wantUp);
            // 移除已经完成的任务
            int count = 0;
            while (count < requestWait.Count())
            {
                if (requestWait[count].hasFinished)
                    requestWait.RemoveAt(count);
                else
                    count++;
            }
        }

        /*检查任务并进行状态转移*/
        public void check_go_ahead(Elevator currentElevator)  // 停靠之后检查是否需要继续
        {
            bool up = false, down = false;
            for (int i = 1; i <= LEVEL_NUM; ++i) 
            {
                if(currentElevator.destination[i])
                {
                    if (i < currentElevator.currentFloor) // 有下降请求
                        down = true;
                    else if (i > currentElevator.currentFloor)  // 有上升请求
                        up = true;
                    else  // 当前处于该楼层
                        currentElevator.destination[i] = false;
                }
            }

            // 查看是否满足了电梯外部请求
            if(whoDealTheDownRequest.ContainsKey(currentElevator.currentFloor) && whoDealTheDownRequest[currentElevator.currentFloor] == currentElevator.no)
            {
                levelDown[currentElevator.currentFloor] = false;
                whoDealTheDownRequest.Remove(currentElevator.currentFloor);
                renew_outside_button();
            }
            else if(whoDealTheUpRequest.ContainsKey(currentElevator.currentFloor) && whoDealTheUpRequest[currentElevator.currentFloor] == currentElevator.no)
            {
                levelUp[currentElevator.currentFloor] = false;
                whoDealTheUpRequest.Remove(currentElevator.currentFloor);
                renew_outside_button();
            }

            // 状态转移
            if (!up && down)
            {
                currentElevator.order = Order.down;
                currentElevator.status = Status.down;
            }
            else if (up && !down)
            {
                currentElevator.order = Order.up;
                currentElevator.status = Status.up;
            }
            else if (currentElevator.order == Order.up && up)
                currentElevator.status = Status.up;  // 继续上升
            else if (currentElevator.order == Order.down && down)
                currentElevator.status = Status.down;  // 继续下降
            else if (!up && !down)
                currentElevator.order = Order.free;
        }

        public void check_pause(Elevator currentElevator)  // 检查当前楼层是否需要停靠
        {
            if(currentElevator.destination[currentElevator.currentFloor])
            {
                currentElevator.destination[currentElevator.currentFloor] = false;  // 取消按钮
                currentElevator.status = Status.pause;
            }
        }

        /*调度算法*/
        public async void add_mission(Elevator currentElevator, int destinationLevel)  // 内部请求调度（分发任务）
        {
            currentElevator.destination[destinationLevel] = true;
            if(currentElevator.order == Order.free)  // 电梯起步
            {
                if (destinationLevel > currentElevator.currentFloor)  // 电梯应上升
                    currentElevator.order = Order.up;
                else if (destinationLevel < currentElevator.currentFloor)  // 电梯应下降
                    currentElevator.order = Order.down;
                else  // 电梯应开门
                {
                    int currentNo = currentElevator.no;
                    timerGroup[currentNo].Enabled = false;
                    elevatorLabel[currentNo].Text += elevatorLabel[currentNo].Text.Contains("open") ? "" : "(open)";
                    await Task.Delay(1000);
                    timerGroup[currentNo].Enabled = true;
                    currentElevator.destination[destinationLevel] = false;
                    elevatorLabel[currentNo].Text = destinationLevel.ToString();
                }
            }
        }

        public async void dispatch(int requestLevel, bool wantUp)  // 外部请求调度
        {
            int chosenElevator = 0;  // 选定响应请求的电梯
            int distance = LEVEL_NUM;
            foreach (KeyValuePair<int, Elevator> kvp in elevatorGroup) 
            {
                bool flag = false;
                Elevator elevator = kvp.Value;
                int currentDistance = elevator.currentFloor - requestLevel;
                if (elevator.order == Order.free && ((wantUp && !whoDealTheDownRequest.ContainsValue(elevator.no))
                    || (!wantUp && !whoDealTheUpRequest.ContainsValue(elevator.no))))   // 电梯空闲，且未被分配不同方向的任务
                {
                    currentDistance = Math.Abs(currentDistance);
                    flag = true;
                }
                // 电梯有上升任务，且请求为上升，且电梯未响应下降请求
                else if (elevator.order == Order.up && wantUp && !whoDealTheDownRequest.ContainsValue(elevator.no))
                {
                    currentDistance = -currentDistance;
                    flag = true;
                }
                // 电梯有下降任务，且请求为下降，且电梯未响应上升请求
                else if (elevator.order == Order.down && !wantUp && !whoDealTheUpRequest.ContainsValue(elevator.no))
                    flag = true;
                if (flag && currentDistance < distance && currentDistance > 0) 
                {
                    distance = currentDistance;
                    chosenElevator = kvp.Key;
                }
                else if(elevator.status == Status.pause && currentDistance == 0)  // 恰好赶上电梯
                {
                    int currentNo = elevator.no;
                    timerGroup[currentNo].Enabled = false;
                    elevatorLabel[currentNo].Text += elevatorLabel[currentNo].Text.Contains("open") ? "" : "(open)";
                    await Task.Delay(1000);
                    timerGroup[currentNo].Enabled = true;
                    elevatorLabel[currentNo].Text = requestLevel.ToString();
                    if (wantUp)
                        levelUp[requestLevel] = false;
                    else
                        levelDown[requestLevel] = false;
                    renew_outside_button();
                    return;
                }
            }

            if (chosenElevator != 0)  // 找到了合适的电梯
            {
                elevatorGroup[chosenElevator].destination[requestLevel] = true;
                // 将任务记录写入表
                if (wantUp && !whoDealTheUpRequest.ContainsKey(requestLevel))
                    whoDealTheUpRequest[requestLevel] = chosenElevator;
                else if (!wantUp && !whoDealTheDownRequest.ContainsKey(requestLevel))
                    whoDealTheDownRequest[requestLevel] = chosenElevator;
                // 检查等待队列中是否有此任务，若有，将其状态改为完成
                Job job;
                job.requestLevel = requestLevel;
                job.wantUp = wantUp;
                job.hasFinished = false;
                if(requestWait.Contains(job))
                {
                    int index = requestWait.IndexOf(job);
                    job.hasFinished = true;
                    requestWait[index] = job;
                }
            }
            else  // 未找到合适的电梯，将任务加入等待队列
            {
                Job waitingJob;
                waitingJob.requestLevel = requestLevel;
                waitingJob.wantUp = wantUp;
                waitingJob.hasFinished = false;

                if(!requestWait.Contains(waitingJob))
                    requestWait.Add(waitingJob);
            }
        }
    }
}
