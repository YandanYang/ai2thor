import prior

from tqdm import tqdm

import random
import math
from ai2thor.controller import Controller
#import ai2thor_colab
from PIL import Image
import numpy as np
import scipy.linalg as linalg
import json
#Image.fromarray(controller.last_event.frame)
import os
import copy
import matplotlib.pyplot as plt
import argparse



def parse_args():
    parser = argparse.ArgumentParser(prog='Generate procthor data.')

    parser.add_argument(
        "--datatype", type=str,  default="3D", help="[\"3D\",\"topdown\",\"multiview\"]"
    )
    
    parser.add_argument(
        "--W", type=int,  default=500, help="image width."
    )
    parser.add_argument(
        "--H", type=int,  default=500, help="image height."
    )

    parser.add_argument(
        "--gridsize", type=float,  default=0.1, help="grid size of the scene."
    )

    parser.add_argument(
        "--renderDepthImage", type=bool, default=True, help="need depth image capture."
    )

    parser.add_argument(
        "--renderNormalsImage", type=bool, default=True, help="need normal image capture."
    )

    parser.add_argument(
        "--frameCnt", type=int,  default=400, help="frame number to capture in this scene."
    )

    parser.add_argument(
        "--local_executable_path", type=str,  default="unity/Build/local-build-procthor.x86_64", help="path of compiled unity path"
    )

    parser.add_argument(
        "--multiview_savepath", type=str,  default="Output/Multiview", help="path to save Multiview data"
    )

    parser.add_argument(
        "--topdown_savepath", type=str,  default="Output/TopDown", help="path to save TopDown data"
    )
   
    args = parser.parse_args()
    return args


def rotate_mat(axis,radian):
    #generate rotation matrix with rotation in 1 axies
    rot_matrix = linalg.expm(np.cross(np.eye(3),axis/linalg.norm(axis)*radian))
    return rot_matrix

def rotate_mat_3axis(radians):
    #generate rotation matrix with rotation in 3 axies
    rot_matrix = np.eye(3)
    rot_matrix = rot_matrix.dot(rotate_mat([1,0,0],radians[0]))
    rot_matrix = rot_matrix.dot(rotate_mat([0,1,0],radians[1]))
    rot_matrix = rot_matrix.dot(rotate_mat([0,0,1],radians[2]))
    return rot_matrix

def get_transform_matrix(radians,T):
    #generate transformation matrix
    rot_matrix = rotate_mat_3axis(radians)
    out = np.eye(4)
    out[:3,:3] = rot_matrix
    out[:3,3] = T
    return out

def to_rad(degree):
    #degree to rad
    return degree * math.pi / 180

def SetFrames(controller,path,args):
    #add camera
    try:
        os.mkdir(path)
    except:
        pass

    event = controller.step(action="GetReachablePositions")
    reachable_positions = event.metadata["actionReturn"]

    FOV = 90
    origin_info = []

    focal_length_x = 0.5 * args.W / math.tan(to_rad(FOV/2))
    focal_length_y = 0.5 * args.H / math.tan(to_rad(FOV/2))
    fl = min(focal_length_x,focal_length_y)
    transform_json = dict(w=args.W,h=args.H,cx=args.W/2,cy=args.H/2,fl_x=fl,fl_y=fl,frames=[])

    for i in range(args.frameCnt):
        position = random.choice(reachable_positions)
        position['y'] = 1 + random.randint(-5,10)/10.0
        rot_x = 0 #random.randint(-40,40)
        rot_y = random.randint(-180,180)
        if i == 0:
            event = controller.step(
                action="AddThirdPartyCamera",
                position=position,
                rotation=dict(x=rot_x, y=rot_y, z=0),
                fieldOfView=FOV
            )
        else:
            event = controller.step(
                    action="UpdateThirdPartyCamera",
                    thirdPartyCameraId=0,
                    rotation=dict(x=rot_x, y=rot_y, z=0),
                    position=position,
                    fieldOfView=FOV
                    )

        origin_info.append([rot_x,rot_y,0]+list(position.values()))
        matrix = get_transform_matrix([to_rad(-rot_x),to_rad(-rot_y),0], [position['x'],position['y'],-position['z']])
        # print(i)
        
        frames = event.third_party_camera_frames
        depth_frames = event.third_party_depth_frames
        file_name = str(i)+".png"
        Image.fromarray(frames[0]).save(path + file_name)
        file_name_depth = str(i)+"_depth.png"
        Image.fromarray(depth_frames[0]).convert('RGB').save(path + file_name_depth)
        info = dict(file_path=file_name, depth_file_path=file_name_depth, transform_matrix=matrix.tolist())
        transform_json["frames"].append(info)

    return transform_json




# with open(path + "origin_info.txt","w") as f:
#     for item in origin_info: 
#         item = [str(i) for i in item]
#         info = " ".join(item)+"/n"
#         f.write(info)


def get_top_down_frame(controller):
    # Setup the top-down camera
    event = controller.step(action="GetMapViewCameraProperties", raise_for_failure=True)
    pose = copy.deepcopy(event.metadata["actionReturn"])
    bounds = event.metadata["sceneBounds"]["size"]
    max_bound = max(bounds["x"], bounds["z"])
    pose["fieldOfView"] = 50
    pose["position"]["y"] += 1.1 * max_bound
    pose["orthographic"] = False
    pose["farClippingPlane"] = 50
    del pose["orthographicSize"]
    # add the camera to the scene
    event = controller.step(
        action="AddThirdPartyCamera",
        **pose,
        skyboxColor="white",
        raise_for_failure=True,
    )
    top_down_frame = event.third_party_camera_frames[-1]
    return Image.fromarray(top_down_frame)

def save_top_down_frame(controller,dataset_index,args):
    
    frame = get_top_down_frame(controller)
    frame.save(args.topdown_savepath+"/"+str(dataset_index)+".png")
    print("saveing top down frame "+str(dataset_index))
    return 

def get_shortest_path_to_point(
    controller, initial_position, target_position, allowed_error=None
):
    """
    Computes the shortest path to a point from an initial position using an agent controller
    :param controller: agent controller
    :param initial_position: dict(x=float, y=float, z=float) with the desired initial rotation
    :param target_position: dict(x=float, y=float, z=float) representing target position
    :param allowed_error: See documentation of the `get_shortest_path_to_object_type` method.
    :return:
    """
    kwargs = dict(
        action="GetShortestPathToPoint",
        position=initial_position,
        target=dict(x=target_position["x"],
                    y=target_position["y"],
                    z=target_position["z"])
    )
    if allowed_error is not None:
        kwargs["allowedError"] = allowed_error

    event = controller.step(kwargs)
    if event.metadata["lastActionSuccess"]:
        return event.metadata["actionReturn"]["corners"]
    else:
        raise ValueError(
            "Unable to find shortest path to point '{}'  due to error '{}'.".format(
                target_position, event.metadata["errorMessage"]
            )
 
        )   
    
def interPolatePath(path,step = 0.03):
    #interpolate path
    path_new = []
    for position in path:
        if path_new==[]:
            path_new.append(position)
            continue
        lastpos = path_new[-1]
        dist = math.sqrt((position['x']-lastpos['x'])*(position['x']-lastpos['x'])+(position['z']-lastpos['z'])*(position['z']-lastpos['z']))
        cnt = int(dist//step + 1)
        dx = (position['x']-path_new[-1]['x'])/cnt
        dz = (position['z']-path_new[-1]['z'])/cnt
        for i in range(1,1+cnt):
            newpos = dict(x=lastpos['x']+dx*i,y=position['y'],z=lastpos['z']+dz*i)
            path_new.append(newpos)
    return path_new


def visualize_path(reachable_positions,path,path_new):
    #reachable_positions
    xs = [rp["x"] for rp in reachable_positions]
    zs = [rp["z"] for rp in reachable_positions]

    fig, ax = plt.subplots(1, 1)
    ax.scatter(xs, zs)
    ax.set_xlabel("$x$")
    ax.set_ylabel("$z$")
    ax.set_title("Reachable Positions in the Scene")
    ax.set_aspect("equal")
    
    # #path,path_new
    # fig, ax = plt.subplots(1, 1)

    xs_path = [p['x'] for p in path_new]
    zs_path = [p['z'] for p in path_new]
    ax.scatter(xs_path, zs_path,c='y')

    xs_path = [p['x'] for p in path]
    zs_path = [p['z'] for p in path]
    ax.scatter(xs_path, zs_path,c='r')

    plt.show()
    return


def genFrameLocation(controller):
    #GetReachablePositions
    event = controller.step(action="GetReachablePositions")
    reachable_positions = event.metadata["actionReturn"]

    #sample start & end position
    position1 = random.choice(reachable_positions)
    position2 = random.choice(reachable_positions)

    #generate path
    path = get_shortest_path_to_point(
        controller = controller,
        initial_position = position1,
        target_position = position2,
        allowed_error=0.0000001
    )   

    #interpolate path
    path_new = interPolatePath(path)

    ##show path
    visualize_path(reachable_positions,path,path_new)
    return path_new
    
def mkdir(dir_path):
    ret = os.system("mkdir -p {}".format(dir_path))
    return ret == 0


def addCamera(controller, Locations, args):
    #add camera
    FOV = 90
    origin_info = []
    
    try:
        os.mkdir(args.multiview_savepath)
    except:
        pass

    focal_length_x = 0.5 * args.W / math.tan(to_rad(FOV/2))
    focal_length_y = 0.5 * args.H / math.tan(to_rad(FOV/2))
    fl = min(focal_length_x,focal_length_y)
    transform_json = dict(w=args.W,h=args.H,cx=args.W/2,cy=args.H/2,fl_x=fl,fl_y=fl,frames=[])
    pbar = tqdm(range(len(Locations)))
    for i in range(len(Locations)):
        position = Locations[i]
        position['y'] = 1 + random.randint(-5,10)/10.0
        rot_x = 0 #random.randint(-40,40)
        rot_y = random.randint(-180,180)
        if i == 0:
            event = controller.step(
                action="AddThirdPartyCamera",
                position=position,
                rotation=dict(x=rot_x, y=rot_y, z=0),
                fieldOfView=FOV
            )
        else:
            event = controller.step(
                    action="UpdateThirdPartyCamera",
                    thirdPartyCameraId=0,
                    rotation=dict(x=rot_x, y=rot_y, z=0),
                    position=position,
                    fieldOfView=FOV
                    )

        origin_info.append([rot_x,rot_y,0]+list(position.values()))
        matrix = get_transform_matrix([to_rad(-rot_x),to_rad(-rot_y),0], [position['x'],position['y'],-position['z']])
        # print(i,len(path_new))
        
        frames = event.third_party_camera_frames
        depth_frames = event.third_party_depth_frames
        file_name = str(i)+".png"
        Image.fromarray(frames[0]).save(args.save_dir +"/"+ file_name)
        file_name_depth = str(i)+"_depth.png"
        Image.fromarray(depth_frames[0]).convert('RGB').save(args.save_dir +"/"+ file_name_depth)
        info = dict(file_path=file_name, depth_file_path=file_name_depth, transform_matrix=matrix.tolist())
        transform_json["frames"].append(info)

        pbar.update(1)
    return transform_json


def getMultiViewFrame(controller,args,dataset_index):
    args.dataset_index = dataset_index
    args.save_dir = args.multiview_savepath + "/" + str(args.dataset_index) 
    mkdir(args.save_dir)
    Locations = genFrameLocation(controller)
    transform_json = addCamera(controller, Locations, args)
    with open(args.save_dir + "/" + "transforms.json","w") as f:
        json.dump(transform_json,f)
    return


def GenerateData(args):
    dataset = prior.load_dataset("procthor-10k")
    datacnt = 10000
    mkdir(args.topdown_savepath)
    for i in range(0,datacnt):
        print("House_"+str(i)+"/")
        house = dataset["train"][i]
       # with open("House_85.json","w") as f:
       #     json.dump(house,f)
        controller = Controller(scene=house,
                                gridsize=args.gridsize,
                                width=args.W,
                                height=args.H,
                                renderDepthImage=args.renderDepthImage, 
                                renderNormalsImage=args.renderNormalsImage,
                                index=i,
                                local_executable_path = args.local_executable_path)
        if (args.datatype=="3D"):
            controller.step(action="SaveHouseToObj",dataset_index=i) 
        elif (args.datatype=="topdown"):
            save_top_down_frame(controller,i,args)
        elif (args.datatype=="multiview"):
            getMultiViewFrame(controller,args,dataset_index=i)
        #transform_json = SetFrames(controller,"gen_data/output/House_"+str(i)+"/",args)
        # with open("gen_data/output/House_"+str(i)+"/" + "transforms.json","w") as f:
        #    json.dump(transform_json,f)
        controller.stop()
    return
        




if __name__ == "__main__":
    #args = dict(
    #    W = 500,
    #    H = 500,
    #    gridsize=0.1,
    #    renderDepthImage = True,
    #    renderNormalsImage=True,
    #    objData = True,
    #    rgbData = True,
    #    NormalData = True,
    #    DepthData = True,
    #    frameCnt = 400,
    #    datatype = "3D", #["3D","topdown","multiview"]
    #    local_executable_path = "/home/yandan/workspace/ai2thor/unity/Build/local-build-procthor.x86_64",
    #    savepath="cube_depth_step03_500_grid01_y180/"
    #)
    
    args = parse_args()
    GenerateData(args)
