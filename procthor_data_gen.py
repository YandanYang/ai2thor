import prior


import random
import math
from ai2thor.controller import Controller
from ai2thor.controller import Controller
import pdb
import ai2thor_colab
from PIL import Image
import numpy as np
import scipy.linalg as linalg
import json
#Image.fromarray(controller.last_event.frame)
import os
import copy
import matplotlib.pyplot as plt


# def get_top_down_frame():
#     # Setup the top-down camera
#     event = controller.step(action="GetMapViewCameraProperties", raise_for_failure=True)
#     pose = copy.deepcopy(event.metadata["actionReturn"])
#     bounds = event.metadata["sceneBounds"]["size"]
#     max_bound = max(bounds["x"], bounds["z"])
#     pose["fieldOfView"] = 50
#     pose["position"]["y"] += 1.1 * max_bound
#     pose["orthographic"] = False
#     pose["farClippingPlane"] = 50
#     del pose["orthographicSize"]
#     # add the camera to the scene
#     event = controller.step(
#         action="AddThirdPartyCamera",
#         **pose,
#         skyboxColor="white",
#         raise_for_failure=True,
#     )
#     top_down_frame = event.third_party_camera_frames[-1]
#     return Image.fromarray(top_down_frame)


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

def SetFrames(controller,path,params):
    #add camera
    try:
        os.mkdir(path)
    except:
        pass

    event = controller.step(action="GetReachablePositions")
    reachable_positions = event.metadata["actionReturn"]

    FOV = 90
    origin_info = []

    focal_length_x = 0.5 * params["W"] / math.tan(to_rad(FOV/2))
    focal_length_y = 0.5 * params["H"] / math.tan(to_rad(FOV/2))
    fl = min(focal_length_x,focal_length_y)
    transform_json = dict(w=params["W"],h=params["H"],cx=params["W"]/2,cy=params["H"]/2,fl_x=fl,fl_y=fl,frames=[])

    for i in range(params["frameCnt"]):
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

def save_top_down_frame(index,controller):
    frame = get_top_down_frame(controller)
    frame.save("top_down_frame/"+str(index)+".png")
    print("saveing top down frame "+str(index))
    return 


def GenerateData(params):
    dataset = prior.load_dataset("procthor-10k")
    datacnt = 10000
    for i in range(1828,datacnt):
        print("House_"+str(i)+"/")
        house = dataset["train"][i]
       # with open("House_85.json","w") as f:
       #     json.dump(house,f)
        controller = Controller(scene=house,
                                gridsize=params["gridsize"],
                                width=params["W"],
                                height=params["H"],
                                renderDepthImage=params["renderDepthImage"], 
                                renderNormalsImage=params["renderNormalsImage"],
                                index=i,
                                local_executable_path = params["local_executable_path"])
        # controller.step(action="SaveHouseToObj",dataset_index=i) 
        save_top_down_frame(i,controller)
        #transform_json = SetFrames(controller,"gen_data/output/House_"+str(i)+"/",params)
        #with open("gen_data/output/House_"+str(i)+"/" + "transforms.json","w") as f:
        #    json.dump(transform_json,f)
        controller.stop()
        




if __name__ == "__main__":
    params = dict(
        W = 500,
        H = 500,
        gridsize=0.1,
        renderDepthImage = True,
        renderNormalsImage=True,
        objData = True,
        rgbData = True,
        NormalData = True,
        DepthData = True,
        frameCnt = 400,
        local_executable_path = "/home/yandan/workspace/ai2thor/unity/Build/local-build-procthor.x86_64"
    )

    GenerateData(params)














    


"""
import zipfile

startdir = "/content/"  #要压缩的文件夹路径
file_news = 'ai2thor.zip' # 压缩后文件夹的名字
z = zipfile.ZipFile(file_news,'w',zipfile.ZIP_DEFLATED) #参数一：文件夹名
for dirpath, dirnames, filenames in os.walk(startdir):
  fpath = dirpath.replace(startdir,'') #这一句很重要，不replace的话，就从根目录开始复制
  fpath = fpath and fpath + os.sep or ''#这句话理解我也点郁闷，实现当前文件夹以及包含的所有文件的压缩
  for filename in filenames:
    if '.png' not in filename and 'transforms' not in filename:
      continue
    z.write(os.path.join(dirpath, filename),fpath+filename)
  print ('success')
z.close()

import shutil
shutil.move(file_news,'/content/ai2thor.zip')
"""
