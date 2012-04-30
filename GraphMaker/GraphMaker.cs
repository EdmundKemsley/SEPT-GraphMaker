﻿#undef DEBUG // #define DEBUG to print debug messages

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GraphMaker
{
    

    public enum Type //Type of transport the stop/station is
    {
        train, tram, bus, unknown
    };

    public class Station
    {
        public string Name { get; set; }
        public Dictionary<int, string> Times { get; set; }
        //public GoogleGeoCodeResponse Geo { get; set; }
        public Station(string name) { this.Name = name; this.Times = new Dictionary<int, string>(); }
    }

    /*
     * Edmund: I am adding this in to try and create a bit of extra functionality or we will
     * nevery know what kind of transport we are using */
    public class Line
    {
        public string name { get; set; }
        public Type type { get; set; }

        public Line(string name, Type type)
        {
            this.name = name;
            this.type = type;
        }
    }

    /*
     * This PathNode class is used to show the path taken in the journey, because some nodes are part of multiple lines,
     * I want to show which line we were on when we passed the node. There is some redundant data here, some forethought
     * and a better design up front would have avoided this, but I'm throwing efficiency out the window at the moment.
     * 
     * The output I'm envisioning will be something like:
     * 
     * Frankston Line : Parkdale Station
     * Frankston Line : Mentone Station
     * Bus 903 : Warrigal / Centre Dandenong Rds
     * Bus 903 : Warrigal / South Rds
     * Bus 903 : Warrigal / North Rds
     * Bus 903 : Oakleigh Station
     * Dandenong Line : Huntingdale Station
     * 
     * and maybe a nicer message like: "Take the Frankston Train from Parkdale to Mentone, then the 903 Bus from Mentone
     * to Oakleigh station, then the Dandenong Train to Huntingdale."
     * 
     */
    public class PathNode
    {
        public GraphNode node { get; set; }
        public string station { get; set; }
        public Line line { get; set; }

        public PathNode(string station, Line line, GraphNode node)
        {
            this.station = station;
            this.line = line;
            this.node = node;
        }
    }

    public class GraphNode
    {   

        public string name { get; set; } // the name of the node
        public List<string> lines; // what lines is this station part of (ie a train line and a bus route)
        public Dictionary<string, Dictionary<string, Station>> adjacency_list; // <line, <next/prev, station>>

        public Dictionary<string, Line> getLineType;

        public GraphNode(Station station, string line, Station next, Station prev)
        {
            name = station.Name; // set the name of the node to the name of the station
            lines = new List<string>(); // this list is kinda redundant, but you can see what lines exist in the graph if you access it
            lines.Add(line); // add this line/route to the list of lines/routes that this station is part of
            adjacency_list = new Dictionary<string, Dictionary<string, Station>>(); // <line, <next/prev, station>>

            getLineType = new Dictionary<string, Line>(); ///////////save a line object as well to save type

            Dictionary<string, Station> adj = new Dictionary<string, Station>(); // <next = Station, prev = Station>
            adj.Add("next", next); // "next" = Station next
            adj.Add("prev", prev); // "prev" = Station prev
            adjacency_list.Add(line, adj);
        }
    }

    public class Graph
    {
        List<GraphNode> graph; // the nodes of the graph, each node is connected by a reference to the next

        public Graph()
        {
            graph = new List<GraphNode>(); // initalise the list
        }

        public void AddLineToGraph(List<Station> line, string line_name, Type type)
        {
            for (int i = 0; i < line.Count; i++) // for each string
            {
                bool node_exists = false;
                Station prev = null, next = null;
                if (i != 0) // not the first node, a previous station exists
                {
                    prev = line[i - 1]; // prev = the previous station in the list
                }
                if (i != (line.Count - 1)) // not the last node, a next station exists
                {
                    next = line[i + 1]; // next = the next station in the list
                }

                // if the node already exists in the graph, we want to add this line/route
                // and these next/prev references to it's adjacency_list rather than
                // add a duplicate node

                foreach (GraphNode n in graph)
                {
                    if (n.name == line[i].Name) // if a node exists with this name
                    {
                        Dictionary<string, Station> adj = new Dictionary<string, Station>(); // new sub-Dict
                        adj.Add("next", next); // set next reference
                        adj.Add("prev", prev); // set prev reference
                        n.adjacency_list.Add(line_name, adj); // add sub-Dict to main-Dict
                        node_exists = true; // set a flag

                        //make temp line obj and add it to graph node as well to track type
                        Line temp = new Line (line_name, type);
                        n.getLineType.Add(line_name, temp);
                    }
                }

                // ADD A NEW NODE, if we ammended an existing node, we skip this part
                if (node_exists == false)
                {
                    GraphNode node = new GraphNode(line[i], line_name, next, prev); // create a new node
                    graph.Add(node); // add it to the graph

                    //make temp line obj and add it to graph node as well to track type
                    Line temp = new Line(line_name, type);
                    node.getLineType.Add(line_name, temp);
                }
            }
        }

        // order of print not necessarily correct, ammended nodes get rearranged in the list
        public void PrintGraph(string line)
        {
            foreach (GraphNode node in graph)
            {
                if (node.adjacency_list.ContainsKey(line)) // if the node is on the specified line/route
                {
                    Console.Write("{0} ", node.name); // print it's name
                    Dictionary<string, Station> adj = node.adjacency_list[line]; // grab the next/prev dict
                    foreach (KeyValuePair<string, Station> pair in adj)
                    {

                        Console.Write("{0} ", pair.Key); // print "next" / "prev"
                        if (pair.Value != null) // first/last station have no reference for prev/next respectively
                        {
                            Console.Write("{0} ", pair.Value.Name); // print the next/prev
                        }
                        else
                        {
                            Console.Write("NONE "); // else print NONE
                        }

                        if (pair.Key == "prev") // if this is the "prev" reference
                        {
                            Console.WriteLine(); // give us a newline
                        }
                    }
                }
            }
        }

        // Breadth First Search
        public bool BFS(Station start, Station end, Type preference)
        {
            // this dictionary contains PathNode pairs, a node and it's previous node, to determine the path
            // a PathNode contains a GraphNode and the line that was used when passing the node.
            Dictionary<PathNode, PathNode> pathnode_path = new Dictionary<PathNode, PathNode>();

            // FIRST CHECK IF THE start NODE EXISTS
            GraphNode root = null;
            bool found = false;
            foreach (GraphNode n in graph) // first check if the station is in the graph
            {
                if (n.name == start.Name)
                {
                    root = n; // store this node as the root node to begin BFS with
                    found = true; // the station does exist
                }
            }
            if (found == false) // the station does NOT exist, return
                return false;
            // END CHECK IF start NODE EXISTS

            // BFS PROPER
            QueueSpace.PriorityQueue<PathNode> pathnode_queue = new QueueSpace.PriorityQueue<PathNode>(); // pathnode_queue stores the line as well as the node
            List<string> visited = new List<string>(); // this list contains the name of ALL nodes that have been visited.

            Line temp = new Line("", Type.unknown);

            pathnode_queue.Enqueue(new PathNode(root.name, temp, root)); // enqueue the root node (starting point)
            visited.Add(root.name); // and mark it as visited (this list keeps track of visited nodes so they don't get visited again)
            pathnode_path.Add(new PathNode(root.name, temp, root), null); // no line given to PathNode just yet

            while (pathnode_queue.Count > 0) // while the queue is not empty, we still have nodes to check
            {
                PathNode pathnode_check = pathnode_queue.Dequeue(); // dequeue the next node
                string typeString = "";

                if (pathnode_check.node.name == end.Name) // if this is the end node, we are done
                {
                    Console.WriteLine("FOUND IT!! Start {0} End {1}\n", start.Name, end.Name); // print a message

                    PathNode pathnode_curr = pathnode_check, pathnode_prev; // set curr to the node we found and init prev to null
                    List<string> pathnode_list_path = new List<string>(); // this list contains the path

                    if (pathnode_curr.line.type == Type.unknown) typeString = "unknown";
                    if (pathnode_curr.line.type == Type.train) typeString = "train";
                    if (pathnode_curr.line.type == Type.bus) typeString = "bus";
                    if (pathnode_curr.line.type == Type.tram) typeString = "tram";

                    pathnode_list_path.Add(pathnode_curr.line.name + " - " + pathnode_curr.station + " via " + typeString); // add the node we just found as the end of the path
                    while (pathnode_curr.node != root) // while we are not back at the first node
                    {
                        if (pathnode_path.TryGetValue(pathnode_curr, out pathnode_prev) == true) // found a value matching the key
                        {
                            if (pathnode_curr.line.type == Type.unknown) typeString = "unknown";
                            if (pathnode_curr.line.type == Type.train) typeString = "train";
                            if (pathnode_curr.line.type == Type.bus) typeString = "bus";
                            if (pathnode_curr.line.type == Type.tram) typeString = "tram";

                            pathnode_list_path.Add(pathnode_curr.line.name + " - " + pathnode_prev.station + " via " + typeString); // add the previous node to the path
                            pathnode_curr = pathnode_prev; // set current node to previous node and repeat
                        }
                        else
                        {
                            Console.WriteLine("Couldn't find the prev pathnode, path broken."); // uh oh
                            break; // finished, we're doomed
                        }
                    }
                    //print the path to console
                    Console.WriteLine();
                    foreach (string s in pathnode_list_path)
                    {
                        Console.WriteLine("Path: {0}", s); // print each 'line - station' in the path
                    }
                    return true;
                }

                // FIND ALL NODES THAT THIS NODE CONNECTS TO AND ADD THEM TO THE QUEUE

                //For enqueuing in order
                List<PathNode> order = new List<PathNode>();

                string[] lines = pathnode_check.node.adjacency_list.Keys.ToArray<string>(); // get all the lines that this node is part of
                foreach (string line in lines)
                {
                    Dictionary<string, Station> adj = pathnode_check.node.adjacency_list[line]; // grab the next/prev dict
                    foreach (KeyValuePair<string, Station> pair in adj) // for next, and prev
                    {
                        if (pair.Value != null) // either next or prev exists
                        {
                            foreach (GraphNode n in graph) // find the station in the graph (this sucks, I have to search the whole graph to find the node again)
                            {
                                if (n.name == pair.Value.Name && visited.Contains(n.name) == false) // if the node name matches the next/prev name we are checking AND we haven't visited it before
                                {
                                    //Used to grab line type
                                    Line value;
                                    if (n.getLineType.ContainsKey(line))
                                    {
                                        value = n.getLineType[line];
                                    }
                                    else //If it can't find the line something is wrong but it won't crash with this here
                                    {
                                        Console.WriteLine("Couldn't find line: " + line);
                                        value = new Line("unknown", Type.unknown);
                                    }

                                    PathNode pathnode_to_queue = new PathNode(n.name, value, n); // create a new PathNode with the node name, line and a copy of the GraphNode


                                    if (preference == Type.unknown) // enqueue it as per usual
                                    {
                                        pathnode_queue.Enqueue(pathnode_to_queue);
                                        
                                    }
                                    else //enqueue in order
                                    {
                                        if (pathnode_to_queue.line.type != preference){ //if it isn't what we prefer
                                            pathnode_queue.Enqueue(pathnode_to_queue);
                                        }
                                        else{
                                            pathnode_queue.Enqueue(pathnode_to_queue, 1);
                                        }
                                    }
                                    
                                    visited.Add(n.name); // mark it as visited
                                    found = true; // flag the station does exist
                                    pathnode_path.Add(pathnode_to_queue, pathnode_check); // add the node, and it's previous node to the path so we can retrace steps at the end
                                }

                            }

                            
                            
                            

                            if (found == false) // the station does NOT exist, return false (this is bad and shouldn't happen, means we have bad references)
                                return false;
                        }
                    }
                    
                }
                //if (preference != Type.unknown)
                //{
                 //   foreach (PathNode insert in order) // Loop through List with foreach
                 //   {
                  //      pathnode_queue.Enqueue(insert);
                  //  }
                //}
                
                
                
            }
            return false; // not found, again, shouldn't happen if we are searching for a station that exists
        }

        public Type unknown { get; set; }
    }

    public class GraphMaker
    {
        static void Main()
        {
            Graph graph = new Graph();
            List<Station> FrankstonLine = new List<Station>();
            List<Station> DandenongLine = new List<Station>();
            List<Station> Bus903 = new List<Station>();

            FrankstonLine.Add(new Station("Parkdale"));
            FrankstonLine.Add(new Station("Mentone"));
            FrankstonLine.Add(new Station("Cheltenham"));
            FrankstonLine.Add(new Station("Highett"));
            FrankstonLine.Add(new Station("Moorabbin"));
            FrankstonLine.Add(new Station("Patterson"));
            FrankstonLine.Add(new Station("Bentleigh"));
            FrankstonLine.Add(new Station("McKinnon"));
            FrankstonLine.Add(new Station("Ormond"));
            FrankstonLine.Add(new Station("Glenhuntly"));
            FrankstonLine.Add(new Station("Caulfield"));

            graph.AddLineToGraph(FrankstonLine, "Frankston Line", Type.train);

            DandenongLine.Add(new Station("Huntingdale"));
            DandenongLine.Add(new Station("Oakleigh"));
            DandenongLine.Add(new Station("Hughesdale"));
            DandenongLine.Add(new Station("Murrumbeena"));
            DandenongLine.Add(new Station("Carnegie"));
            DandenongLine.Add(new Station("Caulfield"));

            graph.AddLineToGraph(DandenongLine, "Dandenong Line", Type.train);

            Bus903.Add(new Station("Warrigal Rd/Princes Hwy"));
            Bus903.Add(new Station("Oakleigh"));
            Bus903.Add(new Station("Warrigal/North Rds"));
            Bus903.Add(new Station("Warrigal/South Rds"));
            Bus903.Add(new Station("Warrigal/Centre Dandenong Rds"));
            Bus903.Add(new Station("Mentone"));

            graph.AddLineToGraph(Bus903, "Bus 903", Type.bus);

            graph.PrintGraph("Frankston Line");
            Console.WriteLine();
            graph.PrintGraph("Dandenong Line");
            Console.WriteLine();
            graph.PrintGraph("Bus 903");

            /*
             * A cycle exists from Mentone >> Caulfield >> Oakleigh >> Mentone
             **/
            Console.WriteLine();
            bool bfs_found = graph.BFS(new Station("Parkdale"), new Station("Huntingdale"), Type.train);

            Console.ReadKey();
        }
    }
}

namespace QueueSpace
{
    public class PriorityQueue<TValue> : PriorityQueue<TValue, int> { }

    public class PriorityQueue<TValue, TPriority> where TPriority : IComparable
    {
        private SortedDictionary<TPriority, Queue<TValue>> dict = new SortedDictionary<TPriority, Queue<TValue>>();

        public int Count { get; private set; }
        public bool Empty { get { return Count == 0; } }

        public void Enqueue(TValue val)
        {
            Enqueue(val, default(TPriority));
        }

        public void Enqueue(TValue val, TPriority pri)
        {
            ++Count;
            if (!dict.ContainsKey(pri)) dict[pri] = new Queue<TValue>();
            dict[pri].Enqueue(val);
        }

        public TValue Dequeue()
        {
            --Count;
            var item = dict.Last();
            if (item.Value.Count == 1) dict.Remove(item.Key);
            return item.Value.Dequeue();
        }
    }
}

